using Serilog.Core;
using Serilog.Events;

namespace Template.Common.Logging;

public sealed class SyslogSeverityEnricher : ILogEventEnricher
{
    private const int FACILITY = 1; // user-level messages

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var severity = logEvent.Level switch
        {
            LogEventLevel.Fatal => 2,       // Critical
            LogEventLevel.Error => 3,       // Error
            LogEventLevel.Warning => 4,     // Warning
            LogEventLevel.Information => 6, // Informational
            LogEventLevel.Debug => 7,       // Debug
            LogEventLevel.Verbose => 7,     // Debug
            _ => 6
        };
        var priority = (FACILITY * 8) + severity;
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SyslogPriority", priority));
    }
}
