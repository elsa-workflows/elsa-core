using Elsa.Common;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Default thread-safe implementation of <see cref="IQuiescenceSignal"/>. Uses a single lock for transitions
/// and a volatile reference read for lock-free state queries. See FR-001..FR-005 and research R8 for
/// pause-persistence semantics.
/// </summary>
public sealed class QuiescenceSignal : IQuiescenceSignal
{
    private const string PersistenceKeyPrefix = "elsa.quiescence.pause.";

    private readonly object _sync = new();
    // Serializes persistence I/O so racing Pause/Resume can't reorder writes in the store. Held only across
    // the I/O — the in-memory transition still uses the fast _sync lock, and pause/resume aren't hot paths.
    private readonly SemaphoreSlim _persistenceMutex = new(1, 1);
    private readonly IOptions<GracefulShutdownOptions> _options;
    private readonly ISystemClock _clock;
    private readonly IKeyValueStore? _keyValueStore;
    private readonly IExecutionCycleRegistry _cycleRegistry;
    private readonly string _persistenceKey;

    private QuiescenceState _state;

    /// <summary>
    /// Creates the signal. The generation id defaults to a new GUID per construction — when the container is torn
    /// down and rebuilt (shell reactivation or host restart), a fresh id is minted, which is what scopes recovery
    /// in <c>RecoverInterruptedWorkflowsStartupTask</c>.
    /// </summary>
    public QuiescenceSignal(
        IOptions<GracefulShutdownOptions> options,
        ISystemClock clock,
        IExecutionCycleRegistry cycleRegistry,
        IKeyValueStore? keyValueStore = null,
        string? shellName = null,
        string? generationId = null)
    {
        _options = options;
        _clock = clock;
        _cycleRegistry = cycleRegistry;
        _keyValueStore = keyValueStore;
        _persistenceKey = PersistenceKeyPrefix + (shellName ?? "default");
        _state = QuiescenceState.Initial(generationId ?? Guid.NewGuid().ToString("N"));
    }

    /// <inheritdoc />
    public QuiescenceState CurrentState
    {
        get
        {
            // Volatile read — the reference is always overwritten atomically under the lock.
            return Volatile.Read(ref _state);
        }
    }

    /// <inheritdoc />
    public bool IsAcceptingNewWork => CurrentState.IsAcceptingNewWork;

    /// <inheritdoc />
    public int ActiveExecutionCycleCount => _cycleRegistry.ActiveCount;

    /// <summary>
    /// Loads any persisted administrative pause state. Called once per runtime generation by a startup task when
    /// <see cref="GracefulShutdownOptions.PausePersistence"/> is <see cref="PausePersistencePolicy.AcrossReactivations"/>.
    /// No-op otherwise, or when the key-value store is not registered.
    /// </summary>
    public async ValueTask InitializePersistedStateAsync(CancellationToken cancellationToken)
    {
        if (_options.Value.PausePersistence != PausePersistencePolicy.AcrossReactivations) return;
        if (_keyValueStore is null) return;

        var pair = await _keyValueStore.FindAsync(new KeyValueFilter { Key = _persistenceKey }, cancellationToken);
        if (pair is null) return;

        lock (_sync)
        {
            if ((_state.Reason & QuiescenceReason.AdministrativePause) != 0) return; // someone already paused us
            var next = _state with
            {
                Reason = _state.Reason | QuiescenceReason.AdministrativePause,
                PausedAt = _clock.UtcNow,
                PauseReasonText = pair.SerializedValue,
                PauseRequestedBy = "persisted",
            };
            Volatile.Write(ref _state, next);
        }
    }

    /// <inheritdoc />
    public ValueTask<QuiescenceState> BeginDrainAsync(CancellationToken cancellationToken = default)
    {
        QuiescenceState next;
        lock (_sync)
        {
            if ((_state.Reason & QuiescenceReason.Drain) != 0)
            {
                return new ValueTask<QuiescenceState>(_state);
            }

            next = _state with
            {
                Reason = _state.Reason | QuiescenceReason.Drain,
                DrainStartedAt = _clock.UtcNow,
            };
            Volatile.Write(ref _state, next);
        }

        return new ValueTask<QuiescenceState>(next);
    }

    /// <inheritdoc />
    public async ValueTask<QuiescenceState> PauseAsync(string? reasonText, string? requestedBy, CancellationToken cancellationToken)
    {
        QuiescenceState next;
        bool transitioned = false;
        lock (_sync)
        {
            if ((_state.Reason & QuiescenceReason.AdministrativePause) != 0)
            {
                next = _state;
            }
            else
            {
                next = _state with
                {
                    Reason = _state.Reason | QuiescenceReason.AdministrativePause,
                    PausedAt = _clock.UtcNow,
                    PauseReasonText = reasonText,
                    PauseRequestedBy = requestedBy,
                };
                Volatile.Write(ref _state, next);
                transitioned = true;
            }
        }

        if (transitioned)
            await PersistAsync();

        return next;
    }

    /// <inheritdoc />
    public async ValueTask<QuiescenceState> ResumeAsync(string? requestedBy, CancellationToken cancellationToken)
    {
        QuiescenceState next;
        bool transitioned = false;
        lock (_sync)
        {
            // Resume is a no-op while drain is active — the runtime cannot return to normal operation within the same generation.
            if ((_state.Reason & QuiescenceReason.Drain) != 0) { return _state; }
            if ((_state.Reason & QuiescenceReason.AdministrativePause) == 0) { return _state; }

            next = _state with
            {
                Reason = _state.Reason & ~QuiescenceReason.AdministrativePause,
                PausedAt = null,
                PauseReasonText = null,
                PauseRequestedBy = requestedBy,
            };
            Volatile.Write(ref _state, next);
            transitioned = true;
        }

        if (transitioned)
            await PersistAsync();

        return next;
    }

    /// <summary>
    /// Persists the current administrative-pause state. Serialized via <see cref="_persistenceMutex"/> so racing
    /// Pause/Resume can't reorder writes in the store. The live state is re-read inside the semaphore — each I/O
    /// writes whatever the latest in-memory transition was, so N racing transitions produce N serialized writes
    /// and the final persisted state always matches the final in-memory state.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="CancellationToken.None"/> deliberately for both the semaphore wait and the store I/O.
    /// By the time this runs the in-memory transition has already committed; if a cancelled HTTP request token
    /// caused the persistence to skip, in-memory state would diverge from the store — and the idempotent
    /// fast-path in <see cref="PauseAsync"/>/<see cref="ResumeAsync"/> (transitioned == false) means a later call
    /// would not retry the write. So persistence must complete regardless of caller cancellation.
    /// </remarks>
    private async ValueTask PersistAsync()
    {
        if (_options.Value.PausePersistence != PausePersistencePolicy.AcrossReactivations || _keyValueStore is null)
            return;

        await _persistenceMutex.WaitAsync(CancellationToken.None);
        try
        {
            var live = Volatile.Read(ref _state);
            if ((live.Reason & QuiescenceReason.AdministrativePause) != 0)
                await _keyValueStore.SaveAsync(new SerializedKeyValuePair { Key = _persistenceKey, SerializedValue = live.PauseReasonText ?? string.Empty }, CancellationToken.None);
            else
                await _keyValueStore.DeleteAsync(_persistenceKey, CancellationToken.None);
        }
        finally
        {
            _persistenceMutex.Release();
        }
    }
}
