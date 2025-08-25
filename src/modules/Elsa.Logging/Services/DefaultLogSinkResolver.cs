using Elsa.Logging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Services;

/// <summary>
/// Default implementation of ILogSinkResolver that resolves sinks from the service provider.
/// </summary>
public class DefaultLogSinkResolver : ILogSinkResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DefaultLogSinkResolver> _logger;
    private readonly Dictionary<string, Func<ILogSink?>> _sinkMappings;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLogSinkResolver"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    public DefaultLogSinkResolver(IServiceProvider serviceProvider, ILogger<DefaultLogSinkResolver> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _sinkMappings = new Dictionary<string, Func<ILogSink?>>
        {
            ["console"] = () => GetSinkByName("console"),
            ["process"] = () => GetSinkByName("console") // Default process sink maps to console
        };
    }

    /// <inheritdoc />
    public IEnumerable<ILogSink> Resolve(IEnumerable<string> sinkNames)
    {
        var resolvedSinks = new List<ILogSink>();

        foreach (var sinkName in sinkNames)
        {
            var sink = Resolve(sinkName);
            if (sink != null)
            {
                resolvedSinks.Add(sink);
            }
        }

        return resolvedSinks;
    }

    /// <inheritdoc />
    public ILogSink? Resolve(string sinkName)
    {
        if (!_sinkMappings.TryGetValue(sinkName, out var sinkFactory))
        {
            _logger.LogWarning("Unknown sink name '{SinkName}'. Available sinks: {AvailableSinks}", 
                sinkName, string.Join(", ", _sinkMappings.Keys));
            return null;
        }

        try
        {
            return sinkFactory();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve sink '{SinkName}'. Error: {Error}", 
                sinkName, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Registers a new sink mapping.
    /// </summary>
    /// <param name="sinkName">The logical sink name.</param>
    /// <param name="sinkFactory">The sink factory function.</param>
    public void RegisterSink(string sinkName, Func<ILogSink?> sinkFactory)
    {
        _sinkMappings[sinkName] = sinkFactory;
    }

    private ILogSink? GetSinkByName(string sinkName)
    {
        // Try to get a sink with the specific name
        var sinks = _serviceProvider.GetServices<ILogSink>();
        return sinks.FirstOrDefault(s => s.Name == sinkName);
    }
}