using Elsa.Logging.Contracts;
using Elsa.Logging.Extensions;
using Elsa.Logging.Sinks;
using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Console;

public sealed class ConsoleLogSinkFactory : ILogSinkFactory<ConsoleLogSinkOptions>
{
    public string Type => "Console";

    public ILogSink Create(string name, ConsoleLogSinkOptions options)
    {
        var factory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddCategoryFilters(options);

            var min = options.MinLevel ?? LogLevel.Information;

            switch (options.Formatter.ToLowerInvariant())
            {
                case "simple":
                    builder.AddSimpleConsole(o =>
                    {
                        if (options.TimestampFormat is not null) o.TimestampFormat = options.TimestampFormat;
                        if (options.ColorBehavior is not null) o.ColorBehavior = options.ColorBehavior.Value;
                        if (options.SingleLine is not null) o.SingleLine = options.SingleLine.Value;
                        if (options.UseUtcTimestamp is not null) o.UseUtcTimestamp = options.UseUtcTimestamp.Value;
                        if (options.IncludeScopes is not null) o.IncludeScopes = options.IncludeScopes.Value;
                    });
                    break;
                case "systemd":
                    builder.AddSystemdConsole(o =>
                    {
                        if (options.TimestampFormat is not null) o.TimestampFormat = options.TimestampFormat;
                        if (options.IncludeScopes is not null) o.IncludeScopes = options.IncludeScopes.Value;
                    });
                    break;
                default:
                    builder.AddConsole(o =>
                    {
                        if (options.TimestampFormat is not null) o.TimestampFormat = options.TimestampFormat;
                        if (options.DisableColors is not null) o.DisableColors = options.DisableColors.Value;
                        if (options.IncludeScopes is not null) o.IncludeScopes = options.IncludeScopes.Value;
                    });
                    break;
            }

            builder.SetMinimumLevel(min);
        });
        return new LoggerSink(name, factory);
    }
}