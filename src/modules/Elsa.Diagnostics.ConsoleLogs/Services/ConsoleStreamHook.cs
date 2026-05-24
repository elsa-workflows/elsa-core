namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Installs a one-time tee on <see cref="Console.Out"/> and <see cref="Console.Error"/> as early as possible (during DI
/// registration) so that downstream logger providers that capture the original writers at construction time
/// (e.g. <c>Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider</c>) end up holding the tee, not the raw writer.
/// Captured text is dispatched to any active <see cref="OnCapture"/> handler.
/// </summary>
public static class ConsoleStreamHook
{
    private static readonly object Lock = new();
    private static bool _installed;
    private static TextWriter? _originalOut;
    private static TextWriter? _originalError;

    /// <summary>
    /// Invoked for every captured chunk written to stdout/stderr. Only one handler should be attached at a time.
    /// </summary>
    public static Action<ConsoleLogStream, string>? OnCapture { get; set; }

    /// <summary>
    /// Installs the tee if not already installed. Safe to call multiple times.
    /// </summary>
    public static void Install()
    {
        lock (Lock)
        {
            if (_installed)
                return;

            _installed = true;
            _originalOut = Console.Out;
            _originalError = Console.Error;
            Console.SetOut(new ConsoleStreamTeeWriter(_originalOut, ConsoleLogStream.Stdout, Dispatch));
            Console.SetError(new ConsoleStreamTeeWriter(_originalError, ConsoleLogStream.Stderr, Dispatch));
        }
    }

    /// <summary>
    /// Restores the original <see cref="Console.Out"/> / <see cref="Console.Error"/> writers. Intended for tests.
    /// </summary>
    public static void Uninstall()
    {
        lock (Lock)
        {
            if (!_installed)
                return;

            if (_originalOut != null)
                Console.SetOut(_originalOut);

            if (_originalError != null)
                Console.SetError(_originalError);

            _originalOut = null;
            _originalError = null;
            _installed = false;
            OnCapture = null;
        }
    }

    private static void Dispatch(ConsoleLogStream stream, string value)
    {
        OnCapture?.Invoke(stream, value);
    }
}

