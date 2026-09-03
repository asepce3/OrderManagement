using Serilog;
using Serilog.Configuration;

namespace OrderManagement.Logging;

public static class EntityFrameworkSinkExtensions
{
    public static LoggerConfiguration EntityFramework(
        this LoggerSinkConfiguration loggerConfiguration,
        IServiceProvider serviceProvider,
        IFormatProvider? formatProvider = null)
    {
        return loggerConfiguration.Sink(new EntityFrameworkSink(serviceProvider, formatProvider));
    }
}
