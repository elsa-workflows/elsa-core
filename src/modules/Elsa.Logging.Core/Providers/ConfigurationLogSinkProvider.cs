using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Logging.Contracts;
using Elsa.Logging.Models;
using Elsa.Logging.Serialization;
using Elsa.Logging.SinkOptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Providers;

public class ConfigurationLogSinkProvider : ILogSinkProvider
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new NullableBoolConverter(),
            new JsonStringEnumConverter()
        }
    };

    private readonly IConfiguration _configuration;
    private readonly IDictionary<string, ILogSinkFactory> _factories;
    private readonly ILogger<ConfigurationLogSinkProvider> _logger;

    public ConfigurationLogSinkProvider(IConfiguration configuration, IEnumerable<ILogSinkFactory> factories, ILogger<ConfigurationLogSinkProvider> logger)
    {
        _configuration = configuration;
        _factories = factories.ToDictionary(x => x.Type);
        _logger = logger;
    }

    public Task<IEnumerable<ILogSink>> GetLogSinksAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetLogSinks());
    }

    private IEnumerable<ILogSink> GetLogSinks()
    {
        var json = _configuration.GetSectionAsJson("LoggingFramework:Sinks");
        var specs = JsonSerializer.Deserialize<List<SinkEnvelope>>(json, _jsonSerializerOptions)!;
        var builtSinks = new List<ILogSink>();

        foreach (var spec in specs)
        {
            var factoryType = spec.Type;

            if (!_factories.TryGetValue(factoryType, out var f))
            {
                _logger.LogWarning("No factory found for type '{Type}'.", factoryType);
                continue;
            }
            
            var sinkOptionsType = f.GetType().GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILogSinkFactory<>)).GetGenericArguments()[0];
            var sinkOptions = spec.Options.Deserialize(sinkOptionsType, _jsonSerializerOptions);
            var createMethod = f.GetType().GetMethod("Create", [typeof(string), sinkOptionsType])!;
            var sink = (ILogSink)createMethod.Invoke(f, [spec.Name, sinkOptions])!;
            builtSinks.Add(sink);
        }

        return builtSinks;
    }
}