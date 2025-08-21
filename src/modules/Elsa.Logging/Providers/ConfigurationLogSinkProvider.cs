using System.Text.Json;
using Elsa.Extensions;
using Elsa.Logging.Contracts;
using Elsa.Logging.Models;
using Elsa.Logging.SinkOptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Logging.Providers;

public class ConfigurationLogSinkProvider(IConfiguration configuration, IServiceProvider serviceProvider) : ILogSinkProvider
{
    public Task<IEnumerable<ILogSink>> GetLogSinksAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetLogSinks());
    }
    
    private IEnumerable<ILogSink> GetLogSinks()
    {
        var json = configuration.GetSectionAsJson("LoggingFramework:Sinks");
        var specs = JsonSerializer.Deserialize<List<SinkEnvelope>>(json, new JsonSerializerOptions{ PropertyNameCaseInsensitive = false})!;
        var consoleFactory = serviceProvider.GetRequiredService<ILogSinkFactory<ConsoleSinkOptions>>();
        var fileFactory = serviceProvider.GetRequiredService<ILogSinkFactory<SerilogFileSinkOptions>>();
        var builtSinks = new List<ILogSink>();
        
        foreach (var spec in specs)
        {
            switch (spec.Type.ToLowerInvariant())
            {
                case "console":
                    var consoleSinkOptions = spec.Options?.Deserialize<ConsoleSinkOptions>()!;
                    builtSinks.Add(consoleFactory.Create(spec.Name, consoleSinkOptions, serviceProvider));
                    break;
                case "serilogfile":
                    var serilogSinkOptions = spec.Options?.Deserialize<SerilogFileSinkOptions>()!;
                    builtSinks.Add(fileFactory.Create(spec.Name, serilogSinkOptions, serviceProvider));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown target type '{spec.Type}'.");
            }
        }
        
        return builtSinks;
    }
}