using Elsa.Python.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Python.Runtime;

namespace Elsa.Python.HostedServices;

/// <summary>
/// Initializes the Python engine.
/// </summary>
public class PythonGlobalInterpreterManager : IHostedService
{
    private readonly IOptions<PythonOptions> _options;
    private IntPtr _mainThreadState;

    /// <summary>
    /// Initializes a new instance of the <see cref="PythonGlobalInterpreterManager"/> class.
    /// </summary>
    public PythonGlobalInterpreterManager(IOptions<PythonOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_options.Value.PythonDllPath)) 
            Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", _options.Value.PythonDllPath);
        
        PythonEngine.Initialize();
        _mainThreadState = PythonEngine.BeginAllowThreads();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        PythonEngine.EndAllowThreads(_mainThreadState);
        PythonEngine.Shutdown();
        return Task.CompletedTask;
    }
}