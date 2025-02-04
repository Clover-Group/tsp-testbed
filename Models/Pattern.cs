using Microsoft.EntityFrameworkCore;

namespace TspTestbed.Models;

[Owned]
public class Pattern
{
    public int Id { get; set; }
    public string SourceCode { get; set; }

    public Dictionary<String, String>? Metadata { get; set; }
    public int? Subunit { get; set; }
}