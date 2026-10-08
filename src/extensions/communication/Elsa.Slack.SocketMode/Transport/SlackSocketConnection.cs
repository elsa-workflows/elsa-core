using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using Elsa.Slack.SocketMode.Events;

namespace Elsa.Slack.SocketMode.Transport;

/// <summary>One physical socket, with one receiver and a bounded, serialized ACK writer. Never reuses a socket slot.</summary>
internal sealed class SlackSocketConnection : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly ClientWebSocket _socket;
    private readonly HttpMessageInvoker _http;
    private readonly SlackSocketModeLimits _limits;
    private readonly SlackSocketWireParser _parser;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Channel<Acknowledgement> _acknowledgements;
    private readonly Task _writer;
    private Task<ReceivedFrame?> _receive = Task.FromResult<ReceivedFrame?>(null);
    private int _pendingAcknowledgements;
    private bool _retired;
    private bool _disposed;

    private SlackSocketConnection(ClientWebSocket socket, HttpMessageInvoker http, SlackSocketModeConfiguration configuration)
    {
        _socket = socket;
        _http = http;
        _limits = configuration.Limits;
        _parser = new(configuration);
        _acknowledgements = Channel.CreateBounded<Acknowledgement>(new BoundedChannelOptions(_limits.MaximumPendingAcknowledgements)
        {
            SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });
        _writer = WriteAcknowledgementsAsync();
    }

    internal static async Task<SlackSocketConnection> ConnectAsync(Uri uri, SlackSocketModeConfiguration configuration,
        SlackSocketTransportPolicy policy, CancellationToken cancellationToken)
    {
        policy.ValidateSocket(uri);
        var socket = new ClientWebSocket();
        // The reviewed .NET 8+ overload accepts our own handler: URL validation alone does not prohibit redirects.
        var http = new HttpMessageInvoker(new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false });
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(configuration.Limits.OperationTimeout);
        try
        {
            await socket.ConnectAsync(uri, http, timeout.Token);
            timeout.Token.ThrowIfCancellationRequested();
            return new(socket, http, configuration);
        }
        catch (Exception)
        {
            socket.Dispose();
            http.Dispose();
            // Connect exceptions can contain the ticket-bearing URL or remote close details.
            throw new InvalidOperationException("socket_connect_failed");
        }
    }

    internal Task<ReceivedFrame?> ReceiveAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_retired)
            {
                return Task.FromResult<ReceivedFrame?>(null);
            }
            if (!_receive.IsCompleted)
            {
                throw new InvalidOperationException("socket_receive_already_active");
            }
            return _receive = ReceiveCoreAsync(cancellationToken);
        }
    }

    private async Task<ReceivedFrame?> ReceiveCoreAsync(CancellationToken cancellationToken)
    {
        using var assembly = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        var bytes = new byte[_limits.MaximumEnvelopeBytes];
        var length = 0;
        try
        {
            for (var fragments = 0; fragments < _limits.MaximumFragments; fragments++)
            {
                var result = await _socket.ReceiveAsync(bytes.AsMemory(length), assembly.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Retire();
                    return null;
                }
                if (result.MessageType != WebSocketMessageType.Text)
                {
                    throw new InvalidDataException();
                }
                length += result.Count;
                if (result.EndOfMessage)
                {
                    var frame = _parser.Parse(bytes.AsMemory(0, length));
                    lock (_gate)
                    {
                        return _retired ? null : new ReceivedFrame(this, frame);
                    }
                }
                if (length == bytes.Length)
                {
                    throw new InvalidDataException();
                }
                if (fragments == 0)
                {
                    // Idle waiting for a message is cancellable; assembling a started message has its own finite deadline.
                    assembly.CancelAfter(_limits.MaximumAssemblyTime);
                }
            }
            throw new InvalidDataException();
        }
        catch (Exception)
        {
            Retire();
            throw new InvalidOperationException("socket_receive_failed");
        }
    }

    /// <summary>False means no successful send is established. Admission must already have committed for every captured member.</summary>
    internal Task<bool> AcknowledgeAsync(ReceivedFrame received, CancellationToken cancellationToken)
    {
        if (!ReferenceEquals(received.Owner, this) || received.Frame.Kind != SlackSocketFrameKind.Event ||
            received.Frame.EnvelopeId is null)
        {
            throw new InvalidOperationException("socket_ack_generation_denied");
        }
        var acknowledgement = new Acknowledgement(received.Frame.EnvelopeId, cancellationToken);
        lock (_gate)
        {
            // Include the current send in the bound, not just channel entries.
            if (_retired || cancellationToken.IsCancellationRequested ||
                _pendingAcknowledgements >= _limits.MaximumPendingAcknowledgements)
            {
                return Task.FromResult(false);
            }
            _pendingAcknowledgements++;
            if (!_acknowledgements.Writer.TryWrite(acknowledgement))
            {
                _pendingAcknowledgements--;
                return Task.FromResult(false);
            }
        }
        return acknowledgement.Completion.Task;
    }

    private async Task WriteAcknowledgementsAsync()
    {
        try
        {
            await foreach (var acknowledgement in _acknowledgements.Reader.ReadAllAsync(_lifetime.Token))
            {
                var sent = false;
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, acknowledgement.CancellationToken);
                    timeout.CancelAfter(_limits.OperationTimeout);
                    var bytes = JsonSerializer.SerializeToUtf8Bytes(new { envelope_id = acknowledgement.EnvelopeId });
                    ValueTask sending;
                    lock (_gate)
                    {
                        if (_retired || timeout.IsCancellationRequested)
                        {
                            continue;
                        }
                        // Retirement and send initiation share this gate. A send begun before retirement may reach the peer;
                        // retirement prohibits every later initiation and aborts the exact old physical socket.
                        sending = _socket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Text, true, timeout.Token);
                    }
                    await sending;
                    sent = true;
                }
                catch (Exception)
                {
                    Retire();
                }
                finally
                {
                    Complete(acknowledgement, sent);
                }
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        finally
        {
            while (_acknowledgements.Reader.TryRead(out var remaining))
            {
                Complete(remaining, false);
            }
        }
    }

    private void Complete(Acknowledgement acknowledgement, bool sent)
    {
        lock (_gate)
        {
            _pendingAcknowledgements--;
        }
        acknowledgement.Completion.TrySetResult(sent);
    }

    internal void Retire()
    {
        lock (_gate)
        {
            if (_retired)
            {
                return;
            }
            _retired = true;
            _acknowledgements.Writer.TryComplete();
            // Keep cancellation/abort ordered before any concurrent drain can dispose the owned resources.
            _lifetime.Cancel();
            _socket.Abort();
        }
    }

    internal async Task<bool> DrainAsync(CancellationToken cancellationToken)
    {
        Retire();
        Task work;
        lock (_gate)
        {
            work = Task.WhenAll(_receive, _writer);
        }
        try
        {
            await work.WaitAsync(_limits.DrainTimeout, cancellationToken);
        }
        catch (Exception)
        {
            // A settled failed receive is drained; a still-running task is not. Never claim a timeout is a stopped socket.
        }
        return work.IsCompleted;
    }

    public async ValueTask DisposeAsync()
    {
        if (!await DrainAsync(CancellationToken.None))
        {
            throw new InvalidOperationException("socket_drain_incomplete");
        }
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _socket.Dispose();
            _http.Dispose();
            _lifetime.Dispose();
        }
    }

    internal sealed class ReceivedFrame(SlackSocketConnection owner, SlackSocketFrame frame)
    {
        internal SlackSocketConnection Owner { get; } = owner;
        internal SlackSocketFrame Frame { get; } = frame;
    }

    private sealed class Acknowledgement(string envelopeId, CancellationToken cancellationToken)
    {
        internal string EnvelopeId { get; } = envelopeId;
        internal CancellationToken CancellationToken { get; } = cancellationToken;
        internal TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
