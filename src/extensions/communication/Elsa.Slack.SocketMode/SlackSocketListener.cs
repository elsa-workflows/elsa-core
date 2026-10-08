using System.Threading.Channels;
using Elsa.Common.Multitenancy;
using Elsa.Slack.SocketMode.Credentials;
using Elsa.Slack.SocketMode.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Slack.SocketMode;

/// <summary>Single host owner of transport generations, durable work and withdrawal.</summary>
internal sealed class SlackSocketListener(SlackSocketModeConfiguration configuration, IServiceScopeFactory scopes,
    SlackSocketTransportPolicy policy, SlackSocketModeHealth health, TimeProvider clock) : IHostedService, IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly TaskCompletionSource _cancellationEnded = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly HashSet<SlackSocketOperation> _operations = [];
    private readonly Channel<bool> _wake = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        SingleReader = true, FullMode = BoundedChannelFullMode.DropWrite, AllowSynchronousContinuations = false
    });
    private readonly SlackSocketDurableWork _durable = new(configuration, scopes, clock);
    private Task? _run;
    private SlackSocketConnection? _connection;
    private SlackSocketSession? _session;
    private bool _stopping;
    private bool _reconciliation;
    private bool _disposed;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_run is not null || _stopping || _disposed)
            {
                throw new InvalidOperationException("socket_listener_already_started");
            }
            // Startup readiness is reported through health; all validation and dependent scopes
            // remain owned even when an audited provider ignores cancellation during startup.
            _run = Task.Run(RunAsync, CancellationToken.None);
        }
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        StopIntake();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(configuration.Limits.DrainTimeout);
        try
        {
            await Task.WhenAll(_run ?? Task.CompletedTask, _cancellationEnded.Task).WaitAsync(deadline.Token);
            if (_session is { IsSettled: false } session && !await session.TryDrainAsync(deadline.Token))
            {
                throw new InvalidOperationException("socket_listener_drain_incomplete");
            }
            await DisposeConnectionAsync();
        }
        catch (Exception)
        {
            MarkReconciliation(SlackSocketModeHealthReason.Drain);
        }
    }

    private async Task RunAsync()
    {
        Task durable = Task.CompletedTask;
        Task ticks = Task.CompletedTask;
        var validated = false;
        try
        {
            health.SetState(SlackSocketModeHealthState.Inactive, SlackSocketModeHealthReason.Provisioning);
            await InScopeAsync(async (services, token) =>
            {
                await services.GetRequiredService<SlackSocketModeHostValidator>().ValidateAsync(token);
                await services.GetRequiredService<SlackSocketEnvelopeProcessor>().DemandCurrentBindingAsync(token);
                return true;
            });
            validated = true;
            if (IsStopping)
            {
                return;
            }
            SignalDurableWork(); // Restart discovery does not depend on a prior in-memory notification.
            durable = RunDurableAsync();
            ticks = TickAsync();
            await RunTransportAsync();
        }
        catch (OperationCanceledException) when (IsStopping)
        {
        }
        catch (Exception)
        {
            MarkReconciliation(SlackSocketModeHealthReason.Provisioning);
        }
        finally
        {
            StopIntake();
            if (validated)
            {
                try
                {
                    // No new intake/dispatch can begin. Withdrawal precedes any future external administration.
                    // A failed host attestation must never invoke an unreviewed store during cleanup.
                    var withdrawn = await InScopeAsync((services, token) =>
                        services.GetRequiredService<SlackSocketSubscriptionWithdrawal>().WithdrawAsync(token), allowStopping: true);
                    if (!withdrawn)
                    {
                        MarkReconciliation(SlackSocketModeHealthReason.Withdrawal);
                    }
                }
                catch (Exception)
                {
                    MarkReconciliation(SlackSocketModeHealthReason.Withdrawal);
                }
            }
            await Task.WhenAll(durable, ticks);
            lock (_gate)
            {
                if (!_reconciliation)
                {
                    health.SetState(SlackSocketModeHealthState.Stopped, SlackSocketModeHealthReason.None);
                }
            }
        }
    }

    private async Task RunTransportAsync()
    {
        var delay = configuration.Limits.ReconnectDelay;
        for (var attempt = 0; attempt < configuration.Limits.MaximumReconnectAttempts && !IsStopping; attempt++)
        {
            if (attempt > 0)
            {
                await Task.Delay(delay, _stop.Token);
                delay = TimeSpan.FromMilliseconds(Math.Min(configuration.Limits.MaximumReconnectDelay.TotalMilliseconds,
                    delay.TotalMilliseconds * 2));
            }
            try
            {
                health.SetState(SlackSocketModeHealthState.Connecting, SlackSocketModeHealthReason.None);
                var opened = await InScopeAsync(async (services, token) =>
                {
                    // Revalidate provisioning, binding and managed generation before EVERY fresh URL open.
                    await services.GetRequiredService<SlackSocketModeHostValidator>().ValidateAsync(token);
                    await services.GetRequiredService<SlackSocketEnvelopeProcessor>().DemandCurrentBindingAsync(token);
                    var reader = services.GetRequiredService<SlackSocketListenerCredentialReader>();
                    var lease = await reader.ResolveCurrentLeaseAsync(token);
                    await reader.DemandCurrentAsync(lease, token);
                    var uri = await services.GetRequiredService<SlackSocketUrlOpener>().OpenAsync(lease.Credential.AccessToken, token);
                    await reader.DemandCurrentAsync(lease, token);
                    await services.GetRequiredService<SlackSocketEnvelopeProcessor>().DemandCurrentBindingAsync(token);
                    token.ThrowIfCancellationRequested();
                    var connection = await SlackSocketConnection.ConnectAsync(uri, configuration, policy, token);
                    lock (_gate)
                    {
                        _connection = connection;
                        if (_stopping)
                        {
                            connection.Retire();
                        }
                    }
                    return (connection, lease);
                });
                if (IsStopping)
                {
                    await DisposeConnectionAsync();
                    return;
                }
                var session = new SlackSocketSession(configuration, scopes, health, SignalDurableWork);
                _session = session;
                var reason = await session.RunAsync(opened.connection, opened.lease, _stop.Token);
                if (!session.IsSettled)
                {
                    MarkReconciliation(SlackSocketModeHealthReason.Drain);
                    return; // Retain this exact session/connection. Never replace an unsettled generation.
                }
                await DisposeConnectionAsync();
                if (reason is SlackSocketSessionEndReason.Stopped or SlackSocketSessionEndReason.LinkDisabled)
                {
                    return;
                }
                if (reason is SlackSocketSessionEndReason.Credentials or SlackSocketSessionEndReason.BindingUnavailable or
                    SlackSocketSessionEndReason.AdmissionUncertainty or SlackSocketSessionEndReason.ReconciliationRequired)
                {
                    MarkReconciliation(SlackSocketModeHealthReason.AdmissionUncertainty);
                    return;
                }
            }
            catch (Exception)
            {
                // A failed open owns no published connection; a successfully published one must drain.
                if (_connection is not null)
                {
                    await DisposeConnectionAsync();
                }
                if (IsStopping)
                {
                    return;
                }
            }
        }
        if (!IsStopping)
        {
            MarkReconciliation(SlackSocketModeHealthReason.Reconnect);
        }
    }

    private async Task RunDurableAsync()
    {
        try
        {
            await foreach (var _ in _wake.Reader.ReadAllAsync(_stop.Token))
            {
                if (IsStopping)
                {
                    return;
                }
                await using var operation = BeginOperation(false);
                try
                {
                    var result = await _durable.RunBatchAsync(operation);
                    if (result.Uncertain)
                    {
                        MarkReconciliation(SlackSocketModeHealthReason.AdmissionUncertainty);
                        return;
                    }
                }
                catch (Exception)
                {
                    // Cancellation during a started durable operation does not prove effects absent.
                    MarkReconciliation(SlackSocketModeHealthReason.AdmissionUncertainty);
                    return;
                }
            }
        }
        catch (Exception)
        {
            if (!IsStopping)
            {
                MarkReconciliation(SlackSocketModeHealthReason.AdmissionUncertainty);
            }
        }
    }

    private async Task TickAsync()
    {
        try
        {
            // Explicit OperationTimeout also bounds the maintenance/restart-discovery cadence.
            using var timer = new PeriodicTimer(configuration.Limits.OperationTimeout);
            while (await timer.WaitForNextTickAsync(_stop.Token))
            {
                SignalDurableWork();
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
        }
    }

    private void SignalDurableWork() => _wake.Writer.TryWrite(true);
    private bool IsStopping { get { lock (_gate) { return _stopping; } } }

    private async Task<T> InScopeAsync<T>(Func<IServiceProvider, CancellationToken, Task<T>> body, bool allowStopping = false)
    {
        await using var operation = BeginOperation(allowStopping);
        await using var scope = scopes.CreateAsyncScope();
        using var tenant = scope.ServiceProvider.GetRequiredService<ITenantAccessor>().PushContext(new Tenant
        {
            Id = configuration.TenantId, Name = configuration.TenantId
        });
        try
        {
            operation.Token.ThrowIfCancellationRequested();
            var result = await body(scope.ServiceProvider, operation.Token);
            operation.Token.ThrowIfCancellationRequested();
            return result;
        }
        finally
        {
            await operation.CloseBodyAsync();
        }
    }

    private SlackSocketOperation BeginOperation(bool allowStopping)
    {
        SlackSocketOperation operation;
        bool stop;
        lock (_gate)
        {
            if (_operations.Count >= 3)
            {
                throw new InvalidOperationException("socket_listener_operation_bound_exceeded");
            }
            operation = new(() => MarkReconciliation(SlackSocketModeHealthReason.Drain), settled =>
            {
                lock (_gate) { _operations.Remove(settled); }
            });
            _operations.Add(operation);
            stop = _stopping && !allowStopping;
        }
        operation.StartDeadline(configuration.Limits.OperationTimeout, () =>
        {
            MarkReconciliation(SlackSocketModeHealthReason.Drain);
            _ = operation.RequestCancellation(); // Withdrawal can begin after the original stop snapshot.
        });
        if (stop)
        {
            _ = operation.RequestCancellation();
        }
        return operation;
    }

    private void MarkReconciliation(SlackSocketModeHealthReason reason)
    {
        lock (_gate)
        {
            _reconciliation = true;
        }
        StopIntake();
        health.LatchReconciliation(reason);
    }

    private void StopIntake()
    {
        SlackSocketOperation[] operations;
        lock (_gate)
        {
            if (_stopping)
            {
                return;
            }
            _stopping = true;
            try
            {
                _connection?.Retire();
            }
            catch (Exception)
            {
                _reconciliation = true;
                health.LatchReconciliation(SlackSocketModeHealthReason.Drain);
            }
            operations = _operations.ToArray();
        }
        _ = CancelOwnedAsync(operations);
    }

    private async Task CancelOwnedAsync(SlackSocketOperation[] operations)
    {
        try
        {
            await Task.WhenAll(operations.Select(operation => operation.RequestCancellation()).Append(_stop.CancelAsync()));
        }
        catch (Exception)
        {
            MarkReconciliation(SlackSocketModeHealthReason.Drain);
        }
        finally
        {
            _cancellationEnded.TrySetResult();
        }
    }

    private async Task DisposeConnectionAsync()
    {
        var connection = _connection;
        if (connection is null)
        {
            return;
        }
        if (_session is { IsSettled: false } || !await connection.DrainAsync(CancellationToken.None))
        {
            MarkReconciliation(SlackSocketModeHealthReason.Drain);
            return;
        }
        await connection.DisposeAsync();
        lock (_gate)
        {
            if (ReferenceEquals(connection, _connection))
            {
                _connection = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
        lock (_gate)
        {
            if (!_disposed && (_run?.IsCompleted ?? true) && _cancellationEnded.Task.IsCompleted &&
                _connection is null && _operations.Count == 0)
            {
                _disposed = true;
                _stop.Dispose();
            }
        }
    }
}
