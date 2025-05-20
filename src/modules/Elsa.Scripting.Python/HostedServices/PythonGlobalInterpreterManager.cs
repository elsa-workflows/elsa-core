using Elsa.Scripting.Python.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Python.Runtime;

namespace Elsa.Scripting.Python.HostedServices;

/// <summary>
/// Initializes the Python engine.
/// </summary>
public class PythonGlobalInterpreterManager : IHostedService
{
    private readonly IOptions<PythonOptions> _options;
    private readonly ILogger _logger;
    private IntPtr _mainThreadState;
    private static bool _initialized;


    /// <summary>
    /// Initializes a new instance of the <see cref="PythonGlobalInterpreterManager"/> class.
    /// </summary>
    public PythonGlobalInterpreterManager(IOptions<PythonOptions> options, ILogger<PythonGlobalInterpreterManager> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return Task.CompletedTask;

        if (!string.IsNullOrEmpty(_options.Value.PythonDllPath))
            Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", _options.Value.PythonDllPath);

        try
        {
            PythonEngine.Initialize();
            _mainThreadState = PythonEngine.BeginAllowThreads();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to initialize Python engine");
        }

        _initialized = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}