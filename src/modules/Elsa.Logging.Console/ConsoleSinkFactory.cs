using Elsa.Logging.Contracts;
using Elsa.Logging.Sinks;
using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Console;

public sealed class ConsoleSinkFactory : ILogSinkFactory<ConsoleSinkOptions>
{
    public string Type => "Console";

    public ILogSink Create(string name, ConsoleSinkOptions options)
    {
        var factory = LoggerFactory.Create(lb =>
        {
            lb.ClearProviders();

            if (options.CategoryFilters is not null)
                foreach (var filter in options.CategoryFilters)
                    lb.AddFilter(filter.Key, filter.Value);

            var min = options.MinLevel ?? LogLevel.Information;

            switch (options.Formatter.ToLowerInvariant())
            {
                case "simple":
                    lb.AddSimpleConsole(o =>
                    {
                        if (options.TimestampFormat is not null) o.TimestampFormat = options.TimestampFormat;
                        if (options.ColorBehavior is not null) o.ColorBehavior = options.ColorBehavior.Value;
                        if (options.SingleLine is not null) o.SingleLine = options.SingleLine.Value;
                        if (options.UseUtcTimestamp is not null) o.UseUtcTimestamp = options.UseUtcTimestamp.Value;
                        if (options.IncludeScopes is not null) o.IncludeScopes = options.IncludeScopes.Value;
                    });
                    break;
                case "systemd":
                    lb.AddSystemdConsole(o =>
                    {
                        if (options.TimestampFormat is not null) o.TimestampFormat = options.TimestampFormat;
                        if (options.IncludeScopes is not null) o.IncludeScopes = options.IncludeScopes.Value;
                    });
                    break;
                default:
                    lb.AddConsole(o =>
                    {
                        if (options.TimestampFormat is not null) o.TimestampFormat = options.TimestampFormat;
                        if (options.DisableColors is not null) o.DisableColors = options.DisableColors.Value;
                        if (options.IncludeScopes is not null) o.IncludeScopes = options.IncludeScopes.Value;
                    });
                    break;
            }

            lb.SetMinimumLevel(min);
        });
        return new LoggerSink(name, factory);
    }
}