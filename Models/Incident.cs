using Microsoft.EntityFrameworkCore;

namespace TspTestbed.Models;

[Owned]
public class Incident
{
    public int PatternId { get; set; }
    public int? Subunit { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is Incident other)
        {
            return this.PatternId == other.PatternId && this.Subunit == other.Subunit
                && this.From == other.From && this.To == other.To;
        }
        else
        {
            return false;
        }
    }
}