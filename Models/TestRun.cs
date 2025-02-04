namespace TspTestbed.Models;

public enum TestRunStatus
{
    Created,
    Running,
    Error,
    Success,
    Failed,
}

public class TestRun
{
    public Guid Id { get; set; }

    public Test Test { get; set; }

    public TimeSpan RunningTime { get; set; }

    public DateTime Started { get; set; }

    public TestRunStatus Status { get; set; }

    public List<Incident> FoundIncidents { get; set; }
}