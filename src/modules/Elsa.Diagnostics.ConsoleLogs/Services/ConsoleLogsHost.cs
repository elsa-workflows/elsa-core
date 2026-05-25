using Elsa.Diagnostics.ConsoleLogs.Providers.InMemory;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Process-wide owner of the console capture pipeline. Console output is, by nature, a single shared OS
/// resource — there is exactly one stdout and one stderr per process — so the capture, the in-memory ring
/// buffer, and the source registry are likewise single instances regardless of how many DI containers
/// (shells, tenants) want to consume them. This class is the seam that makes that fact explicit.
///
/// <para>
/// Configuration follows a first-wins model: <see cref="Configure"/> calls made before the host is first
/// accessed are applied to a single <see cref="ConsoleLogsOptions"/> instance. Subsequent calls are
/// ignored because the configuration would not apply to anything already captured. Call
/// <c>AddConsoleLogsHost</c> from <c>Program.cs</c> to set host-wide options deterministically.
/// </para>
///
/// <para>
/// Initialization is lazy and thread-safe: the first access from any thread builds the pipeline. The host
/// also auto-installs <see cref="ConsoleStreamHook"/>, so simply touching <see cref="Provider"/>,
/// <see cref="SourceRegistry"/>, or <see cref="Capture"/> is enough to start capturing.
/// </para>
/// </summary>
public static class ConsoleLogsHost
{
    private static readonly object Lock = new();
    private static Action<ConsoleLogsOptions>? _pendingConfigure;
    private static Func<IOptions<ConsoleLogsOptions>, IConsoleLogSourceRegistry, IConsoleLogProvider>? _providerFactory;
    private static Lazy<HostState> _state = CreateLazy();

    /// <summary>
    /// Applies a configuration delegate to the host options if the host has not yet been initialized.
    /// Calls after initialization are ignored to preserve consistency between configuration and captured
    /// data. Returns <c>true</c> when the delegate was accepted, <c>false</c> when the host had already
    /// initialized.
    /// </summary>
    public static bool Configure(Action<ConsoleLogsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        lock (Lock)
        {
            if (_state.IsValueCreated)
                return false;

            _pendingConfigure = (Action<ConsoleLogsOptions>?)Delegate.Combine(_pendingConfigure, configure);
            return true;
        }
    }

    /// <summary>
    /// Configures the provider used by the process-wide capture pipeline. The factory must be registered
    /// before the host is initialized.
    /// </summary>
    public static bool ConfigureProvider(Func<IOptions<ConsoleLogsOptions>, IConsoleLogSourceRegistry, IConsoleLogProvider> providerFactory)
    {
        ArgumentNullException.ThrowIfNull(providerFactory);

        lock (Lock)
        {
            if (_state.IsValueCreated)
                return false;

            _providerFactory = providerFactory;
            return true;
        }
    }

    public static IOptions<ConsoleLogsOptions> Options => _state.Value.Options;
    public static IConsoleLogSourceRegistry SourceRegistry => _state.Value.SourceRegistry;
    public static IConsoleLogRedactor Redactor => _state.Value.Redactor;
    public static ConsoleLineFormatter Formatter => _state.Value.Formatter;
    public static ConsoleLogScopeAccessor ScopeAccessor => _state.Value.ScopeAccessor;
    public static IConsoleLogProvider Provider => _state.Value.Provider;
    public static ConsoleCaptureTee Capture => _state.Value.Capture;

    /// <summary>
    /// Triggers initialization without resolving any specific component. Returns <c>true</c> when this call
    /// performed the initialization, <c>false</c> when the host was already initialized.
    /// </summary>
    public static bool EnsureInitialized()
    {
        var wasCreated = _state.IsValueCreated;
        _ = _state.Value;
        return !wasCreated;
    }

    /// <summary>
    /// Tears down the host pipeline. Intended for tests and for orderly host shutdown.
    /// </summary>
    public static async ValueTask ShutdownAsync()
    {
        Lazy<HostState> state;

        lock (Lock)
        {
            state = _state;
            _state = CreateLazy();
            _pendingConfigure = null;
            _providerFactory = null;
        }

        if (state.IsValueCreated)
            await state.Value.Capture.DisposeAsync().ConfigureAwait(false);
    }

    private static Lazy<HostState> CreateLazy() => new(BuildState, LazyThreadSafetyMode.ExecutionAndPublication);

    private static HostState BuildState()
    {
        var options = new ConsoleLogsOptions();

        Func<IOptions<ConsoleLogsOptions>, IConsoleLogSourceRegistry, IConsoleLogProvider>? providerFactory;

        lock (Lock)
        {
            _pendingConfigure?.Invoke(options);
            providerFactory = _providerFactory;
        }

        var wrappedOptions = Microsoft.Extensions.Options.Options.Create(options);
        var sourceRegistry = new ConsoleLogSourceRegistry(wrappedOptions);
        var redactor = new ConsoleLogRedactor(wrappedOptions);
        var formatter = new ConsoleLineFormatter(wrappedOptions);
        var scopeAccessor = new ConsoleLogScopeAccessor();
        var provider = providerFactory?.Invoke(wrappedOptions, sourceRegistry) ?? new InMemoryConsoleLogProvider(wrappedOptions, sourceRegistry);
        var capture = new ConsoleCaptureTee(provider, sourceRegistry, redactor, formatter, scopeAccessor, wrappedOptions);

        return new HostState(wrappedOptions, sourceRegistry, redactor, formatter, scopeAccessor, provider, capture);
    }

    private sealed record HostState(
        IOptions<ConsoleLogsOptions> Options,
        IConsoleLogSourceRegistry SourceRegistry,
        IConsoleLogRedactor Redactor,
        ConsoleLineFormatter Formatter,
        ConsoleLogScopeAccessor ScopeAccessor,
        IConsoleLogProvider Provider,
        ConsoleCaptureTee Capture);
}

