using System.Data.Common;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClickHouse.Ado;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using TspTestbed.Data;
using TspTestbed.Models;

namespace TspTestbed.Services;

public record StatusMessage
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

    private readonly ILogger<RunStatusService> logger;

    private readonly ConsumerConfig config;

    private readonly IConsumer<string, string> consumer;

    private readonly IServiceProvider services;

    private readonly Uri coordinatorUri;

    public event Func<object, Guid, Task> StatusChanged;

    public RunStatusService(IConfiguration configuration, ILogger<RunStatusService> logger, IServiceProvider services)
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
        StatusChanged += (s, e) => LogInvoke(e);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var idx = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var cr = consumer.Consume(stoppingToken);
            var messageValue = cr.Message.Value;
            var message = JsonSerializer.Deserialize<StatusMessage>(messageValue, JsonSerializerOptions.Web);
            logger.LogTrace("Message arrived {Message}", message);
            bool idOk = Guid.TryParse(message?.Uuid, out var runId);
            if (idOk)
            {
                DateTime.TryParse(message?.Timestamp, out DateTime timestamp);
                OnStatusChanged(runId, message?.Status ?? "", timestamp.ToUniversalTime());
            }

            if (idx++ % 1000 == 0)
            {
                consumer.Commit();
            }
        }

        consumer.Close();

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
        logger.LogTrace("Sending request to {Uri}", submitUri);
        var requestAsJson = JsonSerializer.Serialize(request);
        logger.LogTrace("Request content: {Json}", requestAsJson);
        var response = await client.PostAsJsonAsync(submitUri, request);
        logger.LogTrace("Coordinator returned {Response}", response);
        var responseContent = await response.Content.ReadAsStringAsync();
        logger.LogTrace("Response content: {Content}", responseContent);
        if (response.IsSuccessStatusCode)
        {
            run.Status = TestRunStatus.Created;
        }
        else
        {
            run.Status = TestRunStatus.Error;
        }
        appDbContext.SaveChanges();
        StatusChanged?.Invoke(this, runId);
        return runId;
    }

    public void OnStatusChanged(Guid runId, string newStatus, DateTime timestamp)
    {
        logger.LogInformation("Status of {runId} changed to {newStatus} at {timestamp}", runId, newStatus, timestamp);
        using var scope = services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var run = appDbContext.TestRuns.Include(r => r.Test).ThenInclude(t => t.Sink).FirstOrDefault(r => r.Id == runId);
        if (run is not null && run?.Status == TestRunStatus.Created || run?.Status == TestRunStatus.Running)
        {
            switch (newStatus)
            {
                case "RUNNING":
                    run.Status = TestRunStatus.Running;
                    break;
                case "FINISHED":
                    Regex jdbcConnectionRegex = new("jdbc:clickhouse://([\\w\\.]+):([0-9]+)/(\\w+)\\?user=(\\w+)&password=(\\S+)");

                    Match match = jdbcConnectionRegex.Match(run.Test.Sink.JdbcString);
                    if (match.Success)
                    {
                        var host = match.Groups[1].Value;
                        var port = match.Groups[2].Value;
                        var db = match.Groups[3].Value;
                        var username = match.Groups[4].Value;
                        var password = match.Groups[5].Value;
                        var connectionString = $"Host={host};Port={port};Database={db};User={username};Password={password}";
                        logger.LogInformation("Connecting to ClickHouse at: {String}", connectionString);
                        using var connection = new ClickHouseConnection(connectionString);
                        connection.Open();
                        var query = $"SELECT * FROM \"{run.Test.Sink.TableName}\" WHERE run_id = '{run.Id}'";
                        logger.LogInformation("Checking incidents via {Query}", query);
                        var command = connection.CreateCommand(query);
                        using var reader = command.ExecuteReader();
                        List<Incident> incidents = [];
                        var fromIndex = reader.GetOrdinal(FromColumnName);
                        var toIndex = reader.GetOrdinal(ToColumnName);
                        var patternIndex = reader.GetOrdinal(PatternIdColumnName);
                        reader.ReadAll(r =>
                        {
                            incidents.Add(new Incident
                            {
                                From = r.GetDateTime(fromIndex),
                                To = r.GetDateTime(toIndex),
                                PatternId = r.GetInt32(patternIndex),
                            });
                        });
                        logger.LogInformation("Found {Count} incidents", incidents.Count);
                        run.FoundIncidents = incidents;

                        run.Status = CompareIncidents(run.Test.Incidents, incidents) ? TestRunStatus.Success : TestRunStatus.Failed;
                        appDbContext.Entry(run).Collection(r => r.FoundIncidents).IsModified = true;
                        appDbContext.Update(run);
                    }
                    break;
                case "FAILED":
                    run.Status = TestRunStatus.Error;
                    break;
                default: break;
            }
            run.RunningTime = timestamp - run.Started;
            appDbContext.SaveChanges();
            StatusChanged?.Invoke(this, run.Id);
        }
    }

    public override void Dispose()
    {
        consumer.Dispose();
        base.Dispose();
    }

    private bool CompareIncidents(List<Incident> expected, List<Incident> actual)
    {
        if (expected.Count != actual.Count)
        {
            return false;
        }
        var orderedExpected = expected.OrderBy(x => x.PatternId).ThenBy(x => x.From).ThenBy(x => x.To).ToList();
        var orderedActual = actual.OrderBy(x => x.PatternId).ThenBy(x => x.From).ThenBy(x => x.To).ToList();
        return orderedExpected.SequenceEqual(orderedActual);
    }

    private async Task LogInvoke(Guid guid)
    {
        logger.LogInformation("Status changed info fired for {Id}", guid);
    }
}