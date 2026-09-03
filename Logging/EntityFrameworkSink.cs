using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace OrderManagement.Logging;

public class EntityFrameworkSink : ILogEventSink
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFormatProvider? _formatProvider;

    public EntityFrameworkSink(IServiceProvider serviceProvider, IFormatProvider? formatProvider = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var logEntry = MapToLogEntry(logEvent);
            context.LogEntries.Add(logEntry);
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to write log to database: {ex}");
        }
    }

    private LogEntry MapToLogEntry(LogEvent logEvent)
    {
        logEvent.Properties.TryGetValue("CorrelationId", out var correlationIdValue);
        logEvent.Properties.TryGetValue("SourceContext", out var sourceContextValue);
        logEvent.Properties.TryGetValue("RequestPath", out var requestPathValue);
        logEvent.Properties.TryGetValue("RequestMethod", out var requestMethodValue);
        logEvent.Properties.TryGetValue("UserId", out var userIdValue);
        logEvent.Properties.TryGetValue("MachineName", out var machineNameValue);
        logEvent.Properties.TryGetValue("ThreadId", out var threadIdValue);
        logEvent.Properties.TryGetValue("ProcessId", out var processIdValue);

        var message = logEvent.RenderMessage(_formatProvider);
        var exception = logEvent.Exception?.ToString();

        var properties = logEvent.Properties
            .Where(p => p.Key is not "CorrelationId" and not "SourceContext" and not "RequestPath"
                and not "RequestMethod" and not "UserId" and not "MachineName" and not "ThreadId"
                and not "ProcessId")
            .ToDictionary(p => p.Key, p => p.Value.ToString());

        return new LogEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = GetScalarValue(correlationIdValue) ?? string.Empty,
            Timestamp = logEvent.Timestamp.UtcDateTime,
            Level = logEvent.Level.ToString(),
            Message = message,
            Exception = exception,
            SourceContext = GetScalarValue(sourceContextValue) ?? "OrderManagement",
            RequestPath = GetScalarValue(requestPathValue),
            HttpMethod = GetScalarValue(requestMethodValue),
            UserId = ParseNullableInt(GetScalarValue(userIdValue)),
            MachineName = GetScalarValue(machineNameValue) ?? Environment.MachineName,
            ThreadId = ParseInt(GetScalarValue(threadIdValue), Environment.CurrentManagedThreadId),
            ProcessId = ParseInt(GetScalarValue(processIdValue), Environment.ProcessId),
            Properties = properties.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(properties) : null
        };
    }

    private static string? GetScalarValue(LogEventPropertyValue? value)
    {
        if (value is ScalarValue scalar)
            return scalar.Value?.ToString();

        return value?.ToString().Trim('"');
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static int? ParseNullableInt(string? value)
    {
        return int.TryParse(value, out var result) ? result : null;
    }
}
