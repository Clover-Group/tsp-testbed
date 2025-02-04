using Microsoft.EntityFrameworkCore;
using TspTestbed.Models;

namespace TspTestbed.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Source> Sources { get; set; }
    public DbSet<Sink> Sinks { get; set; }
    public DbSet<Test> Tests { get; set; }
    public DbSet<TestRun> TestRuns { get; set; }

    override protected void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Test>().OwnsMany(x => x.Incidents, i => i.ToJson());
        modelBuilder.Entity<Test>().OwnsMany(x => x.Patterns, p => { p.ToJson(); p.OwnsOne(p => p.Metadata); });
        modelBuilder.Entity<TestRun>().OwnsMany(x => x.FoundIncidents, i => i.ToJson());
    }
}