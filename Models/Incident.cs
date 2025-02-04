using Microsoft.EntityFrameworkCore;

namespace TspTestbed.Models;

[Owned]
public class Incident
{
    public int PatternId { get; set; }
    public int? Subunit { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}