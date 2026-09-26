using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Default thread-safe implementation of <see cref="IQuiescenceSignal"/>. Uses a single lock for transitions
/// and a volatile reference read for lock-free state queries. See FR-001..FR-005 and research R8 for
/// pause-persistence semantics.
/// </summary>
public sealed class QuiescenceSignal : IQuiescenceSignal
{
    private const string PersistenceKeyPrefix = "elsa.quiescence.host-pause.";
    private const string LegacyPersistenceKeyPrefix = "elsa.quiescence.pause.";

    private readonly object _sync = new();
    // Held across the whole Pause/Resume (fast-path, transition, persist, revert). Pause/resume aren't hot paths.
    private readonly SemaphoreSlim _persistenceMutex = new(1, 1);
    private readonly IOptions<GracefulShutdownOptions> _options;
    private readonly ISystemClock _clock;
    private readonly IKeyValueStore? _keyValueStore;
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private readonly ITenantAccessor? _tenantAccessor;
    private readonly IExecutionCycleRegistry _cycleRegistry;
    private readonly string _persistenceKey;
    private readonly string _legacyPersistenceKey;

    private static readonly Tenant AgnosticTenant = new()
    {
        Id = Tenant.AgnosticTenantId,
        Name = Tenant.AgnosticTenantId
    };

    private QuiescenceState _state;

    /// <summary>
    /// Creates the signal. The generation id defaults to a new GUID per construction — when the container is torn
    /// down and rebuilt (shell reactivation or host restart), a fresh id is minted, which is what scopes recovery
    /// in <c>RecoverInterruptedWorkflowsStartupTask</c>.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public QuiescenceSignal(
        IOptions<GracefulShutdownOptions> options,
        ISystemClock clock,
        IExecutionCycleRegistry cycleRegistry,
        IServiceScopeFactory serviceScopeFactory,
        string? shellName = null,
        string? generationId = null) : this(options, clock, cycleRegistry, keyValueStore: null, serviceScopeFactory, tenantAccessor: null, shellName, generationId)
    {
    }

    public QuiescenceSignal(
        IOptions<GracefulShutdownOptions> options,
        ISystemClock clock,
        IExecutionCycleRegistry cycleRegistry,
        string? shellName = null,
        string? generationId = null) : this(options, clock, cycleRegistry, keyValueStore: null, serviceScopeFactory: null, tenantAccessor: null, shellName, generationId)
    {
    }

    /// <summary>
    /// Creates the signal with a fixed key-value store. Intended for tests and non-container usage.
    /// </summary>
    public static QuiescenceSignal Create(
        IOptions<GracefulShutdownOptions> options,
        ISystemClock clock,
        IExecutionCycleRegistry cycleRegistry,
        IKeyValueStore? keyValueStore = null,
        string? shellName = null,
        string? generationId = null) => new(options, clock, cycleRegistry, keyValueStore, serviceScopeFactory: null, tenantAccessor: null, shellName, generationId);

    /// <summary>
    /// Creates the signal with a fixed key-value store and tenant accessor. Intended for tests that
    /// exercise tenant-scoped persist through the direct-store path.
    /// </summary>
    public static QuiescenceSignal Create(
        IOptions<GracefulShutdownOptions> options,
        ISystemClock clock,
        IExecutionCycleRegistry cycleRegistry,
        IKeyValueStore keyValueStore,
        ITenantAccessor tenantAccessor,
        string? shellName = null,
        string? generationId = null) => new(options, clock, cycleRegistry, keyValueStore, serviceScopeFactory: null, tenantAccessor, shellName, generationId);

    private QuiescenceSignal(
        IOptions<GracefulShutdownOptions> options,
        ISystemClock clock,
        IExecutionCycleRegistry cycleRegistry,
        IKeyValueStore? keyValueStore,
        IServiceScopeFactory? serviceScopeFactory,
        ITenantAccessor? tenantAccessor,
        string? shellName,
        string? generationId)
    {
        _options = options;
        _clock = clock;
        _cycleRegistry = cycleRegistry;
        _keyValueStore = keyValueStore;
        _serviceScopeFactory = serviceScopeFactory;
        _tenantAccessor = tenantAccessor;
        var shell = shellName ?? "default";
        _persistenceKey = PersistenceKeyPrefix + shell;
        _legacyPersistenceKey = LegacyPersistenceKeyPrefix + shell;
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

        var pair = await FindAsync(_persistenceKey, AgnosticTenant, cancellationToken);
        if (pair is null)
            pair = await AdoptLegacyPauseAsync(cancellationToken);
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
        await _persistenceMutex.WaitAsync(CancellationToken.None);
        try
        {
            QuiescenceState next;
            QuiescenceState previous;
            lock (_sync)
            {
                previous = _state;
                if ((_state.Reason & QuiescenceReason.AdministrativePause) != 0)
                    return _state;

                next = _state with
                {
                    Reason = _state.Reason | QuiescenceReason.AdministrativePause,
                    PausedAt = _clock.UtcNow,
                    PauseReasonText = reasonText,
                    PauseRequestedBy = requestedBy,
                };
                Volatile.Write(ref _state, next);
            }

            try
            {
                await PersistUnlockedAsync();
            }
            catch
            {
                RevertPauseFields(previous);
                throw;
            }

            return next;
        }
        finally
        {
            _persistenceMutex.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask<QuiescenceState> ResumeAsync(string? requestedBy, CancellationToken cancellationToken)
    {
        await _persistenceMutex.WaitAsync(CancellationToken.None);
        try
        {
            QuiescenceState next;
            QuiescenceState previous;
            lock (_sync)
            {
                // Resume is a no-op while drain is active — the runtime cannot return to normal operation within the same generation.
                if ((_state.Reason & QuiescenceReason.Drain) != 0) return _state;
                if ((_state.Reason & QuiescenceReason.AdministrativePause) == 0) return _state;

                previous = _state;
                next = _state with
                {
                    Reason = _state.Reason & ~QuiescenceReason.AdministrativePause,
                    PausedAt = null,
                    PauseReasonText = null,
                    PauseRequestedBy = requestedBy,
                };
                Volatile.Write(ref _state, next);
            }

            try
            {
                await PersistUnlockedAsync();
            }
            catch
            {
                RevertPauseFields(previous);
                throw;
            }

            return next;
        }
        finally
        {
            _persistenceMutex.Release();
        }
    }

    /// <summary>
    /// Writes the current administrative-pause flag. Caller must hold <see cref="_persistenceMutex"/>.
    /// The live state is re-read here so the I/O matches the latest in-memory transition.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="CancellationToken.None"/> deliberately. By the time this runs the in-memory
    /// transition has already committed; if a cancelled HTTP request token skipped the write, the
    /// idempotent fast-path would not retry. Persistence must complete regardless of caller cancellation.
    /// </remarks>
    private async ValueTask PersistUnlockedAsync()
    {
        if (_options.Value.PausePersistence != PausePersistencePolicy.AcrossReactivations)
            return;

        // Legacy EF rows used elsa.quiescence.pause.{shell} and may be stamped '' or a named tenant.
        // Memory starts empty on restart, so the leftover-row risk is EF-only. The host-pause key
        // never shares that PK; adoption at startup copies a default-tenant/NULL legacy row.
        await UseKeyValueStoreAsync(async store =>
        {
            var live = Volatile.Read(ref _state);
            if ((live.Reason & QuiescenceReason.AdministrativePause) != 0)
            {
                await store.SaveAsync(new SerializedKeyValuePair
                {
                    Key = _persistenceKey,
                    SerializedValue = live.PauseReasonText ?? string.Empty,
                    TenantId = Tenant.AgnosticTenantId
                }, CancellationToken.None);
            }
            else
            {
                await store.DeleteAsync(_persistenceKey, CancellationToken.None);
            }
        }, AgnosticTenant);
    }

    private void RevertPauseFields(QuiescenceState previous)
    {
        lock (_sync)
        {
            var current = Volatile.Read(ref _state);
            var restored = current with
            {
                Reason = (current.Reason & ~QuiescenceReason.AdministrativePause) | (previous.Reason & QuiescenceReason.AdministrativePause),
                PausedAt = previous.PausedAt,
                PauseReasonText = previous.PauseReasonText,
                PauseRequestedBy = previous.PauseRequestedBy,
            };
            Volatile.Write(ref _state, restored);
        }
    }

    /// <summary>
    /// One-time upgrade: a 3.8 default-tenant or NULL row on the old key is visible under
    /// <see cref="Tenant.Default"/>. Copy it to the host-pause <c>*</c> key and delete the old row.
    /// Named-tenant leftover rows stay on the old key and cannot collide.
    /// </summary>
    private async Task<SerializedKeyValuePair?> AdoptLegacyPauseAsync(CancellationToken cancellationToken)
    {
        var legacy = await FindAsync(_legacyPersistenceKey, Tenant.Default, cancellationToken);
        if (legacy is null) return null;

        await UseKeyValueStoreAsync(store => store.SaveAsync(new SerializedKeyValuePair
        {
            Key = _persistenceKey,
            SerializedValue = legacy.SerializedValue,
            TenantId = Tenant.AgnosticTenantId
        }, cancellationToken), AgnosticTenant);
        await UseKeyValueStoreAsync(store => store.DeleteAsync(_legacyPersistenceKey, cancellationToken), Tenant.Default);
        return legacy;
    }

    private ValueTask<SerializedKeyValuePair?> FindAsync(string key, Tenant tenant, CancellationToken cancellationToken) =>
        UseKeyValueStoreAsync(store => store.FindAsync(new KeyValueFilter { Key = key }, cancellationToken), defaultValue: (SerializedKeyValuePair?)null, tenant);

    private async ValueTask UseKeyValueStoreAsync(Func<IKeyValueStore, Task> action, Tenant tenant)
    {
        await UseKeyValueStoreAsync(async store =>
        {
            await action(store);
            return true;
        }, defaultValue: false, tenant);
    }

    private async ValueTask<TResult> UseKeyValueStoreAsync<TResult>(Func<IKeyValueStore, Task<TResult>> action, TResult defaultValue, Tenant tenant)
    {
        if (_keyValueStore is not null)
        {
            using var _ = _tenantAccessor?.PushContext(tenant);
            return await action(_keyValueStore);
        }

        if (_serviceScopeFactory is null)
            return defaultValue;

        using var scope = _serviceScopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetService<IKeyValueStore>();
        if (store is null)
            return defaultValue;

        var accessor = scope.ServiceProvider.GetService<ITenantAccessor>();
        using var __ = accessor?.PushContext(tenant);
        return await action(store);
    }
}
