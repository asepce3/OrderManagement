namespace OrderManagement.Models;

public class LogEntry
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string SourceContext { get; set; } = string.Empty;
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public int? UserId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public int ThreadId { get; set; }
    public int ProcessId { get; set; }
    public string? Properties { get; set; }
}
