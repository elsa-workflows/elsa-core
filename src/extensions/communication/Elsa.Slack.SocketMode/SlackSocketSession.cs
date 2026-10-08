using System.Threading.Channels;
using Elsa.Common.Multitenancy;
using Elsa.Slack.SocketMode.Credentials;
using Elsa.Slack.SocketMode.Events;
using Elsa.Slack.SocketMode.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Slack.SocketMode;

internal enum SlackSocketSessionEndReason
{
    Stopped, RefreshRequested, LinkDisabled, Transport, Protocol, Backpressure, Credentials,
    BindingUnavailable, AdmissionRejected, AdmissionUncertainty, ReconciliationRequired
}

/// <summary>
/// One physical generation, one bounded intake queue and one admission processor. The notification
/// must be synchronous, nonblocking and coalesced by the durable-work dispatcher; it conveys no permit.
/// An incomplete drain retains this session and its connection. It never permits replacement/disposal.
/// </summary>
internal sealed class SlackSocketSession(SlackSocketModeConfiguration configuration, IServiceScopeFactory scopes,
    SlackSocketModeHealth health, Action notifyDurableWork)
{
    private readonly object _gate = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly TaskCompletionSource _ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _hello = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Channel<SlackSocketConnection.ReceivedFrame> _frames = Channel.CreateBounded<SlackSocketConnection.ReceivedFrame>(
        new BoundedChannelOptions(configuration.Limits.MaximumPendingEnvelopes)
        {
            SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });
    private SlackSocketConnection? _connection;
    private Task? _work;
    private CancellationTokenRegistration _externalCancellation;
    private SlackSocketSessionEndReason _terminationReason;
    private int _started;
    private int _draining;
    private int _settled;
    private int _queued;
    private int _inflight;
    private bool _ending;

    internal bool IsSettled => Volatile.Read(ref _settled) == 1;
    internal SlackSocketSessionEndReason TerminationReason
    {
        get
        {
            lock (_gate)
            {
                return _terminationReason;
            }
        }
    }

    internal async Task<SlackSocketSessionEndReason> RunAsync(SlackSocketConnection connection, SlackSocketCredentialLease lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(lease);
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
        {
            throw new InvalidOperationException("socket_session_already_started");
        }
        _connection = connection;
        health.SetState(SlackSocketModeHealthState.Connecting, SlackSocketModeHealthReason.None);
        _externalCancellation = cancellationToken.Register(() => End(SlackSocketSessionEndReason.Stopped));
        // Keep every operation, including ignored cancellation and async scope disposal, owned until unwind.
        _work = Task.WhenAll(PumpAsync(connection), ProcessAsync(connection, lease), RevalidatePeriodicallyAsync(lease));
        await _ended.Task;
        return await TryDrainAsync(CancellationToken.None) ? TerminationReason : SlackSocketSessionEndReason.ReconciliationRequired;
    }

    internal async Task<bool> TryDrainAsync(CancellationToken cancellationToken)
    {
        if (IsSettled)
        {
            return true;
        }
        if (_work is null || _connection is null || !_ended.Task.IsCompleted)
        {
            throw new InvalidOperationException("socket_session_not_ending");
        }
        if (Interlocked.CompareExchange(ref _draining, 1, 0) != 0)
        {
            throw new InvalidOperationException("socket_session_drain_already_active");
        }
        try
        {
            // Both waits share ONE deadline, rather than spending DrainTimeout twice sequentially.
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(configuration.Limits.DrainTimeout);
            var connectionDrain = _connection.DrainAsync(deadline.Token);
            try
            {
                await Task.WhenAll(_work.WaitAsync(deadline.Token), connectionDrain);
            }
            catch (Exception)
            {
                // A settled failure is drained; an incomplete operation retains its scope/resources.
            }
            if (!_work.IsCompleted || !connectionDrain.IsCompletedSuccessfully || !connectionDrain.Result)
            {
                health.SetState(SlackSocketModeHealthState.ReconciliationRequired, SlackSocketModeHealthReason.Drain);
                return false;
            }
            _externalCancellation.Dispose();
            _stop.Dispose();
            Volatile.Write(ref _settled, 1);
            return true;
        }
        finally
        {
            Volatile.Write(ref _draining, 0);
        }
    }

    private async Task PumpAsync(SlackSocketConnection connection)
    {
        var hello = false;
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                SlackSocketConnection.ReceivedFrame? received;
                if (!hello)
                {
                    using var handshake = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                    using var expired = handshake.Token.Register(() => End(SlackSocketSessionEndReason.Protocol));
                    handshake.CancelAfter(configuration.Limits.OperationTimeout);
                    received = await connection.ReceiveAsync(handshake.Token);
                }
                else
                {
                    received = await connection.ReceiveAsync(_stop.Token);
                }
                if (received is null)
                {
                    End(SlackSocketSessionEndReason.Transport);
                    return;
                }
                if (!ReferenceEquals(received.Owner, connection))
                {
                    End(SlackSocketSessionEndReason.Protocol);
                    return;
                }
                switch (received.Frame.Kind)
                {
                    case SlackSocketFrameKind.Hello when !hello:
                        hello = true;
                        lock (_gate)
                        {
                            if (!_ending)
                            {
                                health.SetState(SlackSocketModeHealthState.Connected, SlackSocketModeHealthReason.None);
                                _hello.TrySetResult();
                            }
                        }
                        break;
                    case SlackSocketFrameKind.Refresh:
                        End(SlackSocketSessionEndReason.RefreshRequested);
                        return;
                    case SlackSocketFrameKind.LinkDisabled:
                        End(SlackSocketSessionEndReason.LinkDisabled);
                        return;
                    case SlackSocketFrameKind.Event when hello && received.Frame.Event is not null && received.Frame.EnvelopeId is not null:
                        lock (_gate)
                        {
                            if (_ending)
                            {
                                return;
                            }
                            if (!_frames.Writer.TryWrite(received))
                            {
                                End(SlackSocketSessionEndReason.Backpressure);
                                return;
                            }
                            _queued++;
                            UpdateWorkCounts();
                        }
                        break;
                    default:
                        End(SlackSocketSessionEndReason.Protocol);
                        return;
                }
            }
        }
        catch (Exception)
        {
            // Physical receive intentionally hides whether a parser or network error occurred.
            End(_stop.IsCancellationRequested ? SlackSocketSessionEndReason.Stopped : SlackSocketSessionEndReason.Transport);
        }
        finally
        {
            _frames.Writer.TryComplete();
        }
    }

    private async Task ProcessAsync(SlackSocketConnection connection, SlackSocketCredentialLease lease)
    {
        try
        {
            await foreach (var received in _frames.Reader.ReadAllAsync(_stop.Token))
            {
                lock (_gate)
                {
                    _queued--;
                    if (_ending)
                    {
                        UpdateWorkCounts();
                        return;
                    }
                    _inflight = 1; // Deliberately serialized, within every valid configured concurrency bound.
                    UpdateWorkCounts();
                }
                try
                {
                    await ProcessFrameAsync(connection, lease, received);
                }
                finally
                {
                    lock (_gate)
                    {
                        _inflight = 0;
                        UpdateWorkCounts();
                    }
                }
            }
        }
        catch (Exception)
        {
            if (!_stop.IsCancellationRequested)
            {
                SignalDurableWork();
                End(SlackSocketSessionEndReason.AdmissionUncertainty);
            }
        }
        finally
        {
            lock (_gate)
            {
                while (_frames.Reader.TryRead(out _))
                {
                    _queued--;
                }
                UpdateWorkCounts();
            }
        }
    }

    private async Task ProcessFrameAsync(SlackSocketConnection connection, SlackSocketCredentialLease lease,
        SlackSocketConnection.ReceivedFrame received)
    {
        var failure = (int)SlackSocketSessionEndReason.Credentials;
        var attemptedAdmission = false;
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
        using var expired = operation.Token.Register(() =>
        {
            var reason = (SlackSocketSessionEndReason)Volatile.Read(ref failure);
            End(reason);
            if (reason == SlackSocketSessionEndReason.AdmissionUncertainty)
            {
                // A dependency may ignore cancellation indefinitely after committing. Wake recovery now,
                // while still retaining that dependency/scope; its eventual unwind may signal again.
                SignalDurableWork();
            }
        });
        operation.CancelAfter(configuration.Limits.OperationTimeout);
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var services = scope.ServiceProvider;
            using var tenant = services.GetRequiredService<ITenantAccessor>().PushContext(new Tenant
            {
                Id = configuration.TenantId, Name = configuration.TenantId
            });
            var credentials = services.GetRequiredService<SlackSocketListenerCredentialReader>();
            var processor = services.GetRequiredService<SlackSocketEnvelopeProcessor>();
            await credentials.DemandCurrentAsync(lease, operation.Token);
            operation.Token.ThrowIfCancellationRequested();
            Volatile.Write(ref failure, (int)SlackSocketSessionEndReason.AdmissionUncertainty);
            attemptedAdmission = true;
            var batch = await processor.ProcessAsync(received.Frame.Event!, operation.Token);
            health.RecordOutcome(SlackSocketModeHealthOutcome.Admitted, batch.Admitted);
            health.RecordOutcome(SlackSocketModeHealthOutcome.Discarded, batch.Discarded);
            health.RecordOutcome(SlackSocketModeHealthOutcome.Duplicate, batch.Duplicates);
            if (batch.AdmissionIds.Count > 0 && !SignalDurableWork())
            {
                return;
            }
            operation.Token.ThrowIfCancellationRequested();
            if (batch.Outcome != SlackSocketBatchOutcome.Committed)
            {
                health.RecordOutcome(SlackSocketModeHealthOutcome.Rejected);
                End(batch.Outcome switch
                {
                    SlackSocketBatchOutcome.Inactive => SlackSocketSessionEndReason.BindingUnavailable,
                    SlackSocketBatchOutcome.CapacityExceeded => SlackSocketSessionEndReason.Backpressure,
                    _ => SlackSocketSessionEndReason.AdmissionRejected
                });
                return;
            }
            Volatile.Write(ref failure, (int)SlackSocketSessionEndReason.Credentials);
            await credentials.DemandCurrentAsync(lease, operation.Token);
            Volatile.Write(ref failure, (int)SlackSocketSessionEndReason.BindingUnavailable);
            await processor.DemandCurrentBindingAsync(operation.Token);
            operation.Token.ThrowIfCancellationRequested();
            Task<bool> acknowledged;
            lock (_gate)
            {
                if (_ending || operation.IsCancellationRequested || !ReferenceEquals(received.Owner, connection))
                {
                    return;
                }
                Volatile.Write(ref failure, (int)SlackSocketSessionEndReason.Transport);
                // End/retirement and ACK initiation share this gate; no queued frame starts a later send after termination.
                acknowledged = received.Owner.AcknowledgeAsync(received, operation.Token);
            }
            if (!await acknowledged)
            {
                End(SlackSocketSessionEndReason.Backpressure);
            }
        }
        catch (Exception)
        {
            if (attemptedAdmission)
            {
                // Discover the actual durable ledger, including commit-then-throw. Never fabricate a successful result.
                SignalDurableWork();
            }
            End((SlackSocketSessionEndReason)Volatile.Read(ref failure));
        }
    }

    private async Task RevalidatePeriodicallyAsync(SlackSocketCredentialLease lease)
    {
        // OperationTimeout is the explicit maximum idle check interval AND each check's liveness budget.
        // PeriodicTimer coalesces ticks during a check, avoiding an extra full interval after a slow check.
        try
        {
            await _hello.Task.WaitAsync(_stop.Token);
            using var timer = new PeriodicTimer(configuration.Limits.OperationTimeout);
            while (await timer.WaitForNextTickAsync(_stop.Token))
            {
                var failure = (int)SlackSocketSessionEndReason.Credentials;
                using var operation = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                using var expired = operation.Token.Register(() => End((SlackSocketSessionEndReason)Volatile.Read(ref failure)));
                operation.CancelAfter(configuration.Limits.OperationTimeout);
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var services = scope.ServiceProvider;
                    using var tenant = services.GetRequiredService<ITenantAccessor>().PushContext(new Tenant
                    {
                        Id = configuration.TenantId, Name = configuration.TenantId
                    });
                    await services.GetRequiredService<SlackSocketListenerCredentialReader>().DemandCurrentAsync(lease, operation.Token);
                    Volatile.Write(ref failure, (int)SlackSocketSessionEndReason.BindingUnavailable);
                    await services.GetRequiredService<SlackSocketEnvelopeProcessor>().DemandCurrentBindingAsync(operation.Token);
                    operation.Token.ThrowIfCancellationRequested();
                }
                catch (Exception)
                {
                    End((SlackSocketSessionEndReason)Volatile.Read(ref failure));
                    return;
                }
            }
        }
        catch (Exception)
        {
            End(_stop.IsCancellationRequested ? SlackSocketSessionEndReason.Stopped : SlackSocketSessionEndReason.Credentials);
        }
    }

    private bool SignalDurableWork()
    {
        try
        {
            notifyDurableWork();
            return true;
        }
        catch (Exception)
        {
            End(SlackSocketSessionEndReason.AdmissionUncertainty);
            return false;
        }
    }

    private void End(SlackSocketSessionEndReason reason)
    {
        lock (_gate)
        {
            if (_ending)
            {
                return;
            }
            _ending = true;
            _terminationReason = reason;
            // Retire before cancellation can unwind a different worker into queued ACK initiation.
            try
            {
                _connection!.Retire();
            }
            catch (Exception)
            {
                _terminationReason = SlackSocketSessionEndReason.ReconciliationRequired;
                reason = SlackSocketSessionEndReason.ReconciliationRequired;
            }
            try
            {
                _stop.Cancel();
            }
            catch (AggregateException)
            {
                // A trusted policy's cancellation callback cannot export raw details or suppress the drain boundary.
                _terminationReason = SlackSocketSessionEndReason.ReconciliationRequired;
                reason = SlackSocketSessionEndReason.ReconciliationRequired;
            }
            var state = reason switch
            {
                SlackSocketSessionEndReason.Stopped or SlackSocketSessionEndReason.RefreshRequested or SlackSocketSessionEndReason.LinkDisabled => SlackSocketModeHealthState.Stopped,
                SlackSocketSessionEndReason.Backpressure => SlackSocketModeHealthState.Backpressured,
                SlackSocketSessionEndReason.Credentials or SlackSocketSessionEndReason.BindingUnavailable or SlackSocketSessionEndReason.AdmissionUncertainty or SlackSocketSessionEndReason.ReconciliationRequired => SlackSocketModeHealthState.ReconciliationRequired,
                _ => SlackSocketModeHealthState.Faulted
            };
            var healthReason = reason switch
            {
                SlackSocketSessionEndReason.Stopped => SlackSocketModeHealthReason.Drain,
                SlackSocketSessionEndReason.RefreshRequested => SlackSocketModeHealthReason.Reconnect,
                SlackSocketSessionEndReason.LinkDisabled or SlackSocketSessionEndReason.BindingUnavailable => SlackSocketModeHealthReason.Withdrawal,
                SlackSocketSessionEndReason.Transport => SlackSocketModeHealthReason.Transport,
                SlackSocketSessionEndReason.Protocol => SlackSocketModeHealthReason.Protocol,
                SlackSocketSessionEndReason.Backpressure => SlackSocketModeHealthReason.Backpressure,
                SlackSocketSessionEndReason.Credentials => SlackSocketModeHealthReason.Credentials,
                SlackSocketSessionEndReason.AdmissionUncertainty => SlackSocketModeHealthReason.AdmissionUncertainty,
                SlackSocketSessionEndReason.ReconciliationRequired => SlackSocketModeHealthReason.Drain,
                _ => SlackSocketModeHealthReason.Configuration
            };
            health.SetState(state, healthReason);
            _ended.TrySetResult();
        }
    }

    // Caller holds _gate, keeping queued/inflight snapshots coherent with queue mutation and retirement.
    private void UpdateWorkCounts() => health.SetWorkCounts(_queued, _inflight);
}
