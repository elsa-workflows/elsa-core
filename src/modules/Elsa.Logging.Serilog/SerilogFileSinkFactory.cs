using Elsa.Logging.Contracts;
using Elsa.Logging.Sinks;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

namespace Elsa.Logging.Serilog;

public sealed class SerilogFileSinkFactory : ILogSinkFactory<SerilogFileSinkOptions>
{
    public string Type => "SerilogFile";

    public ILogSink Create(string name, SerilogFileSinkOptions options)
    {
        var cfg = new LoggerConfiguration().MinimumLevel.Verbose();

        if (string.Equals(options.Formatter, "CompactJson", StringComparison.OrdinalIgnoreCase))
        {
            cfg = cfg.WriteTo.File(new CompactJsonFormatter(), options.Path,
                rollingInterval: MapRolling(options.RollingInterval),
                retainedFileCountLimit: options.RetentionCount);
        }
        else
        {
            cfg = cfg.WriteTo.File(options.Path,
                rollingInterval: MapRolling(options.RollingInterval),
                retainedFileCountLimit: options.RetentionCount,
                outputTemplate: string.IsNullOrWhiteSpace(options.Template)
                    ? "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                    : options.Template);
        }

        var serilog = cfg.CreateLogger();
        var factory = LoggerFactory.Create(lb =>
        {
            lb.ClearProviders();
            lb.AddSerilog(serilog, dispose: true);

            if (options.CategoryFilters is not null)
                foreach (var filter in options.CategoryFilters)
                    lb.AddFilter(filter.Key, filter.Value);

            lb.SetMinimumLevel(options.MinLevel ?? LogLevel.Information);
        });

        return new LoggerSink(name, factory);
    }

    static RollingInterval MapRolling(string? v) => Enum.TryParse<RollingInterval>(v, true, out var ri) ? ri : RollingInterval.Day;
}