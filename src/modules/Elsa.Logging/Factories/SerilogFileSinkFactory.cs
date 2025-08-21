using Elsa.Logging.Contracts;
using Elsa.Logging.SinkOptions;
using Elsa.Logging.Sinks;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

namespace Elsa.Logging.Factories;

public sealed class SerilogFileSinkFactory : ILogSinkFactory<SerilogFileSinkOptions>
{
    public string Type => "SerilogFile";

    public ILogSink Create(string name, SerilogFileSinkOptions opt, IServiceProvider sp)
    {
        var cfg = new LoggerConfiguration().MinimumLevel.Verbose();

        if (string.Equals(opt.Formatter, "CompactJson", StringComparison.OrdinalIgnoreCase))
        {
            cfg = cfg.WriteTo.File(new CompactJsonFormatter(), opt.Path,
                rollingInterval: MapRolling(opt.RollingInterval),
                retainedFileCountLimit: opt.RetentionCount);
        }
        else
        {
            cfg = cfg.WriteTo.File(opt.Path,
                rollingInterval: MapRolling(opt.RollingInterval),
                retainedFileCountLimit: opt.RetentionCount,
                outputTemplate: string.IsNullOrWhiteSpace(opt.Template)
                    ? "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                    : opt.Template);
        }

        var serilog = cfg.CreateLogger();
        var privateFactory = LoggerFactory.Create(lb =>
        {
            lb.ClearProviders();
            lb.AddSerilog(serilog, dispose: true);
            lb.SetMinimumLevel(opt.MinLevel ?? LogLevel.Information);
        });

        return new MelLogSink(name, privateFactory, opt.DefaultCategory ?? "Elsa.Activity");
    }

    static RollingInterval MapRolling(string? v) => Enum.TryParse<RollingInterval>(v, true, out var ri) ? ri : RollingInterval.Day;
}