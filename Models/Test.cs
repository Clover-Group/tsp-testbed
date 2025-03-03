namespace TspTestbed.Models;

public class Test
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Source Source { get; set; }
    public Sink Sink { get; set; }

    public string Query { get; set; }
    public string DatetimeField { get; set; }
    public uint ChunkSizeMs { get; set; }
    public uint EventsMaxGapMs { get; set; }
    public uint DefaultEventsGapMs { get; set; }

    public List<TestRun> Runs { get; set; }

    public List<Pattern> Patterns { get; set; }

    public List<Incident> Incidents { get; set; }
}