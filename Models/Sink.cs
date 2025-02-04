namespace TspTestbed.Models;

public class Sink
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string JdbcString { get; set; }
    public string TableName { get; set; }
}