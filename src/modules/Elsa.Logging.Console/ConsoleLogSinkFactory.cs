using Elsa.Logging.Contracts;
using Elsa.Logging.Extensions;
using Elsa.Logging.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Elsa.Logging.Console;

/// <summary>
/// A log sink factory implementation for creating console-based log sinks.
/// </summary>
/// <remarks>
/// This factory is responsible for setting up and configuring console-based log sinks using
/// the <see cref="ConsoleLogSinkOptions"/> provided. It supports multiple formatter configurations
/// like "simple", "systemd", or default console logging. It also allows customization of aspects
/// such as timestamp format, color behavior, and log level.
/// </remarks>
/// <seealso cref="ILogSinkFactory{ConsoleLogSinkOptions}" />
public sealed class ConsoleLogSinkFactory : ILogSinkFactory<ConsoleLogSinkOptions>
{
    /// <inheritdoc/>
    public string Type => "Console";

    public ILogSink Create(string name, ConsoleLogSinkOptions options)
    {
        var factory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddCategoryFilters(options);
            
            builder.Services.Configure<ConsoleFormatterOptions>(cfo =>
            {
                if (options.TimestampFormat is not null) cfo.TimestampFormat = options.TimestampFormat;
                if (options.IncludeScopes is not null) cfo.IncludeScopes = options.IncludeScopes.Value;
                if (options.UseUtcTimestamp is not null) cfo.UseUtcTimestamp = options.UseUtcTimestamp.Value;
            });

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
                case "json":
                    builder.AddConsoleFormatter<JsonDestructuringConsoleFormatter, ConsoleFormatterOptions>();
                    builder.AddConsole(o =>
                    {
                        o.FormatterName = JsonDestructuringConsoleFormatter.FormatterName;
                    });
                    break;
                default:
                    builder.AddConsole();
                    break;
            }

            builder.SetMinimumLevel(min);
        });
        return new LoggerSink(name, factory);
    }
}