namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Installs a process-wide tee on <see cref="Console.Out"/> and <see cref="Console.Error"/> so that every
/// captured write is fanned out to all currently registered subscribers, in addition to the original writers.
///
/// <para>
/// Downstream logger providers (e.g. <c>Microsoft.Extensions.Logging.Console</c>) capture <c>Console.Out</c>
/// once at construction. Therefore the tee must be installed BEFORE any such provider is constructed —
/// ideally as the very first line of <c>Program.cs</c>. Calling <see cref="Subscribe"/> implicitly installs
/// the tee, so a host can also rely on subscribers attaching early in the pipeline.
/// </para>
///
/// <para>
/// Writes captured before any subscriber attaches are kept in a bounded replay buffer and delivered to the
/// first subscriber (when <c>replayPreviousWrites</c> is true), preserving original timestamps. This bridges
/// the gap between <see cref="Install"/> and the first <see cref="Subscribe"/> call, which typically happens
/// when the DI container is built and the capture singleton is resolved.
/// </para>
///
/// <para>
/// Subscribers that themselves write to the console while handling a capture would cause infinite recursion.
/// Set <see cref="SuppressCapture"/> to true on the publish path's <see cref="AsyncLocal{T}"/> context to
/// short-circuit dispatch for the duration of that asynchronous flow.
/// </para>
/// </summary>
public static class ConsoleStreamHook
{
    private static readonly object Lock = new();
    private static readonly AsyncLocal<bool> SuppressFlag = new();
    private static volatile Subscription[] _subscriptions = [];
    private static readonly ReplayBuffer Replay = new(capacity: 4096);
    private static volatile bool _installed;
    private static TextWriter? _originalOut;
    private static TextWriter? _originalError;

    /// <summary>
    /// While true on the current async context, captured writes will not be dispatched to subscribers.
    /// Subscribers must set this to true while running their publish pipeline so that any logging triggered
    /// by that pipeline does not feed back into capture.
    /// </summary>
    public static bool SuppressCapture
    {
        get => SuppressFlag.Value;
        set => SuppressFlag.Value = value;
    }

    /// <summary>
    /// Installs the tee if not already installed. Safe to call multiple times.
    /// </summary>
    public static void Install()
    {
        if (_installed)
            return;

        lock (Lock)
        {
            if (_installed)
                return;

            _originalOut = Console.Out;
            _originalError = Console.Error;
            Console.SetOut(new ConsoleStreamTeeWriter(_originalOut, ConsoleLogStream.Stdout, Dispatch));
            Console.SetError(new ConsoleStreamTeeWriter(_originalError, ConsoleLogStream.Stderr, Dispatch));
            _installed = true;
        }
    }

    /// <summary>
    /// Restores the original writers and clears all subscribers. Intended for tests.
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
            _subscriptions = [];
            Replay.Clear();
            _installed = false;
        }
    }

    /// <summary>
    /// Registers a subscriber that receives every captured write. Disposing the returned token removes the
    /// subscription. Calling Subscribe auto-installs the tee. When <paramref name="replayPreviousWrites"/>
    /// is true, the buffered writes captured since install are replayed to this subscriber synchronously.
    /// </summary>
    public static IDisposable Subscribe(Action<CapturedChunk> handler, bool replayPreviousWrites = true)
    {
        ArgumentNullException.ThrowIfNull(handler);

        Install();
        var subscription = new Subscription(handler);

        lock (Lock)
        {
            _subscriptions = [.._subscriptions, subscription];

            if (replayPreviousWrites)
            {
                foreach (var chunk in Replay.Snapshot())
                    InvokeSafely(subscription, chunk);

                Replay.Clear();
            }
        }

        return subscription;
    }

    internal static void Remove(Subscription subscription)
    {
        lock (Lock)
        {
            _subscriptions = _subscriptions.Where(x => !ReferenceEquals(x, subscription)).ToArray();
        }
    }

    private static void Dispatch(ConsoleLogStream stream, string value)
    {
        if (SuppressFlag.Value || value.Length == 0)
            return;

        var chunk = new CapturedChunk(stream, value, DateTimeOffset.UtcNow);
        var subscriptions = _subscriptions;

        if (subscriptions.Length == 0)
        {
            // No subscriber yet — retain for replay to the first one that attaches.
            Replay.Add(chunk);
            return;
        }

        foreach (var subscription in subscriptions)
            InvokeSafely(subscription, chunk);
    }

    private static void InvokeSafely(Subscription subscription, CapturedChunk chunk)
    {
        try
        {
            subscription.Handler(chunk);
        }
        catch (Exception e) when (!IsCriticalException(e))
        {
            // Never let a subscriber take down the writer. Subscribers are responsible for their own resilience.
        }
    }

    private static bool IsCriticalException(Exception exception) =>
        exception is OutOfMemoryException
            or StackOverflowException
            or AccessViolationException
            or AppDomainUnloadedException
            or BadImageFormatException
            or CannotUnloadAppDomainException;

    internal sealed class Subscription(Action<CapturedChunk> handler) : IDisposable
    {
        public Action<CapturedChunk> Handler { get; } = handler;
        public void Dispose() => Remove(this);
    }

    private sealed class ReplayBuffer(int capacity)
    {
        private readonly object _lock = new();
        private readonly Queue<CapturedChunk> _items = new(capacity);

        public void Add(CapturedChunk chunk)
        {
            lock (_lock)
            {
                if (_items.Count == capacity)
                    _items.Dequeue();

                _items.Enqueue(chunk);
            }
        }

        public IReadOnlyList<CapturedChunk> Snapshot()
        {
            lock (_lock)
                return _items.ToArray();
        }

        public void Clear()
        {
            lock (_lock)
                _items.Clear();
        }
    }
}

/// <summary>
/// A single chunk of text captured from stdout/stderr together with the moment of capture.
/// </summary>
public readonly record struct CapturedChunk(ConsoleLogStream Stream, string Text, DateTimeOffset TimestampUtc);
