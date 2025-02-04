using System.Text.Json;
using ClickHouse.Ado;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using TspTestbed.Data;
using TspTestbed.Models;

namespace TspTestbed.Services;

public class StatusMessage
{
    public string Uuid { get; set; } = default!;
    public string Timestamp { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string Text { get; set; } = default!;
}

public class RunStatusService : BackgroundService
{
    public const string ClickHouseDriverName = "com.github.housepower.jdbc.ClickHouseDriver";
    public const string PatternIdColumnName = "pattern_id";
    public const string FromColumnName = "from_ts";
    public const string ToColumnName = "to_ts";
    public const string RunIdColumnName = "run_id";
    private readonly IConfiguration configuration;

    private readonly ILogger<BackgroundService> logger;

    private readonly ConsumerConfig config;

    private readonly IConsumer<string, string> consumer;

    private readonly IServiceProvider services;

    private readonly Uri coordinatorUri;

    public RunStatusService(IConfiguration configuration, ILogger<BackgroundService> logger, IServiceProvider services)
    {
        this.configuration = configuration;
        this.logger = logger;
        this.services = services;
        coordinatorUri = configuration.GetSection("Coordinator").GetValue<Uri>("Uri");
        config = new ConsumerConfig
        {
            BootstrapServers = configuration.GetSection("Coordinator").GetValue<string>("JobStatusBroker"),
            GroupId = configuration.GetSection("Coordinator").GetValue<string>("ConsumerGroup"),
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        consumer = new ConsumerBuilder<string, string>(config).Build();
        var topic = configuration.GetSection("Coordinator").GetValue<string>("JobStatusTopic");
        consumer.Subscribe(topic);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var idx = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var cr = consumer.Consume(stoppingToken);
            var messageValue = cr.Message.Value;
            var message = JsonSerializer.Deserialize<StatusMessage>(messageValue);
            bool idOk = Guid.TryParse(message?.Uuid, out var runId);
            if (idOk)
            {
                DateTime.TryParse(message?.Timestamp, out DateTime timestamp);
                OnStatusChanged(runId, message?.Status ?? "", timestamp);
            }

            if (idx++ % 1000 == 0)
            {
                consumer.Commit();
            }
        }

    }

    public async Task<Guid> RunTest(Guid testId)
    {
        using var scope = services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        using var client = clientFactory.CreateClient();
        var runId = Guid.NewGuid();
        var test = appDbContext.Tests.Include(t => t.Source).Include(t => t.Sink).FirstOrDefault(t => t.Id == testId);
        var run = new TestRun()
        {
            Id = runId,
            Status = TestRunStatus.Created,
            Started = DateTime.UtcNow,
            Test = test,
            RunningTime = TimeSpan.Zero
        };
        appDbContext.TestRuns.Add(run);
        appDbContext.SaveChanges();
        var request = new
        {
            Uuid = runId,
            Source = new
            {
                Type = "jdbc",
                Config = new
                {
                    JdbcUrl = test.Source.JdbcString,
                    test.Query,
                    DriverName = ClickHouseDriverName,
                    test.DatetimeField,
                    PartitionFields = Array.Empty<object>()
                }
            },
            Sinks = (List<object>)[
                new
                {
                    Type = "jdbc",
                    Config = new
                    {
                        JdbcUrl = test.Sink.JdbcString,
                        test.Sink.TableName,
                        DriverName = ClickHouseDriverName,
                        RowSchema = new
                        {
                            Data = new Dictionary<string, object>
                            {
                                [PatternIdColumnName] = new
                                {
                                    Type = "int32",
                                    Value = "$PatternID"
                                },
                                [FromColumnName] = new
                                {
                                    Type = "timestamp",
                                    Value = "$IncidentStart"
                                },
                                [ToColumnName] = new
                                {
                                    Type = "timestamp",
                                    Value = "$IncidentEnd",
                                },
                                [RunIdColumnName] = new
                                {
                                    Type = "string",
                                    Value = $"{runId}"
                                }
                            }
                        }
                    }
                }
            ],
            Patterns = test.Patterns.Select(p => new
            {
                p.Id,
                p.SourceCode,
                p.Subunit,
                p.Metadata
            }).ToList()
        };
        var submitUri = new Uri(coordinatorUri, "/api/v3/job/submit/");
        logger.LogError("Sending request to {Uri}", submitUri);
        var requestAsJson = JsonSerializer.Serialize(request);
        logger.LogError("Request content: {Json}", requestAsJson);
        var response = await client.PostAsJsonAsync(submitUri, request);
        logger.LogError("Coordinator returned {Response}", response);
        var responseContent = await response.Content.ReadAsStringAsync();
        logger.LogError("Response content: {Content}", responseContent);
        if (response.IsSuccessStatusCode)
        {
            run.Status = TestRunStatus.Running;
        }
        else
        {
            run.Status = TestRunStatus.Error;
        }
        appDbContext.SaveChanges();
        return runId;
    }

    public void OnStatusChanged(Guid runId, string newStatus, DateTime timestamp)
    {
        using var scope = services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var run = appDbContext.TestRuns.FirstOrDefault(r => r.Id == runId);
        if (run is not null && run?.Status == TestRunStatus.Created || run?.Status == TestRunStatus.Running)
        {
            switch (newStatus)
            {
                case "FINISHED":
                    run.Status = TestRunStatus.Success;
                    break;
                case "FAILED":
                    run.Status = TestRunStatus.Error;
                    break;
                default: break;
            }
            run.RunningTime = timestamp - run.Started;
            appDbContext.SaveChanges();
        }
    }

    public override void Dispose()
    {
        consumer.Dispose();
        base.Dispose();
    }
}