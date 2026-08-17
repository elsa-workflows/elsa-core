using System.Net;
using System.Net.Sockets;
using Elsa.ExternalAuthentication.Options;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Validation;

/// <summary>
/// Resolves and validates provider-controlled destinations before any outbound request or connection is made.
/// </summary>
public sealed class OutboundDestinationValidator(IOptions<ExternalAuthenticationOptions> options, IOutboundDnsResolver dnsResolver)
{
    public async ValueTask<IReadOnlyCollection<IPAddress>> ValidateAsync(Uri destination, CancellationToken cancellationToken = default)
    {
        ValidateUri(destination);
        var addresses = destination.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6
            ? [IPAddress.Parse(destination.Host)]
            : await dnsResolver.ResolveAsync(destination.DnsSafeHost, cancellationToken);

        if (addresses.Count == 0 || !options.Value.ProviderEgress.AllowPrivateNetworkDestinations && addresses.Any(IsUnsafeAddress))
            throw new OutboundDestinationException();

        return addresses;
    }

    public async ValueTask<IPAddress> ResolveApprovedAddressAsync(DnsEndPoint endpoint, CancellationToken cancellationToken = default)
    {
        var destination = new UriBuilder(Uri.UriSchemeHttps, endpoint.Host, endpoint.Port).Uri;
        var addresses = await ValidateAsync(destination, cancellationToken);
        return addresses.First();
    }

    public void ValidateApprovedProxy(Uri proxyUri)
    {
        if (!proxyUri.IsAbsoluteUri ||
            !string.Equals(proxyUri.Scheme, "http", StringComparison.OrdinalIgnoreCase) && !string.Equals(proxyUri.Scheme, "https", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(proxyUri.UserInfo) ||
            !string.IsNullOrEmpty(proxyUri.Fragment) ||
            proxyUri.HostNameType == UriHostNameType.Unknown)
            throw new OutboundDestinationException();
    }

    private void ValidateUri(Uri destination)
    {
        var policy = options.Value.ProviderEgress;
        if (!destination.IsAbsoluteUri ||
            policy.RequireHttps && !string.Equals(destination.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(destination.UserInfo) ||
            !string.IsNullOrEmpty(destination.Fragment) ||
            destination.HostNameType == UriHostNameType.Unknown)
            throw new OutboundDestinationException();

        var allowedHosts = policy.AllowedHosts ?? [];
        if (allowedHosts.Count > 0 && !allowedHosts.Any(host => string.Equals(host.TrimEnd('.'), destination.DnsSafeHost.TrimEnd('.'), StringComparison.OrdinalIgnoreCase)))
            throw new OutboundDestinationException();
    }

    private static bool IsUnsafeAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 0 ||
                bytes[0] == 10 ||
                bytes[0] == 100 && bytes[1] is >= 64 and <= 127 ||
                bytes[0] == 127 ||
                bytes[0] == 169 && bytes[1] == 254 ||
                bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
                bytes[0] == 192 && (bytes[1] == 0 || bytes[1] == 168) ||
                bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2 ||
                bytes[0] == 198 && (bytes[1] is 18 or 19 || bytes[1] == 51 && bytes[2] == 100) ||
                bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113 ||
                bytes[0] >= 224;
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
            return true;

        var ipv6 = address.GetAddressBytes();
        return IPAddress.IsLoopback(address) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.IsIPv6LinkLocal ||
            address.IsIPv6SiteLocal ||
            address.IsIPv6Multicast ||
            ipv6[0] is 0xfc or 0xfd ||
            ipv6[0] == 0x20 && ipv6[1] == 0x01 && ipv6[2] == 0x0d && ipv6[3] == 0xb8;
    }
}

public interface IOutboundDnsResolver
{
    ValueTask<IReadOnlyCollection<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken = default);
}

public sealed class SystemOutboundDnsResolver : IOutboundDnsResolver
{
    public async ValueTask<IReadOnlyCollection<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken = default) => await Dns.GetHostAddressesAsync(host, cancellationToken);
}

public sealed class OutboundDestinationException : InvalidOperationException
{
    public OutboundDestinationException() : base("The provider destination is not permitted.")
    {
    }
}
