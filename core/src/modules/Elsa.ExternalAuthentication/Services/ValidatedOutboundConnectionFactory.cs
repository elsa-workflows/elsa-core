using System.Net;
using System.Net.Sockets;
using Elsa.ExternalAuthentication.Validation;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Pins each outbound connection to an address that was resolved and validated at connection time.
/// </summary>
public sealed class ValidatedOutboundConnectionFactory(OutboundDestinationValidator destinationValidator, IValidatedAddressConnector connector)
{
    public async ValueTask<Stream> ConnectAsync(DnsEndPoint endpoint, CancellationToken cancellationToken = default)
    {
        var address = await destinationValidator.ResolveApprovedAddressAsync(endpoint, cancellationToken);
        return await connector.ConnectAsync(address, endpoint.Port, cancellationToken);
    }
}

public interface IValidatedAddressConnector
{
    ValueTask<Stream> ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken = default);
}

public sealed class SocketValidatedAddressConnector : IValidatedAddressConnector
{
    public async ValueTask<Stream> ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken = default)
    {
        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(new IPEndPoint(address, port), cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
