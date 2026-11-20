using System.Net;
using System.Net.Sockets;
using Elsa.Http.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Services;

/// <summary>
/// Downloads files from explicitly configured public HTTPS destinations.
/// </summary>
public class HttpClientFileDownloader : IFileDownloader
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<HttpActivityOptions> _options;

    /// <summary>
    /// Initializes a downloader with the default policy, which denies remote destinations.
    /// </summary>
    public HttpClientFileDownloader(HttpClient httpClient) : this(httpClient, Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions()))
    {
    }

    /// <summary>
    /// Initializes the downloader with the host's remote download configuration.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public HttpClientFileDownloader(HttpClient httpClient, IOptions<HttpActivityOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    internal static SocketsHttpHandler CreateHandler() => new()
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        UseCookies = false,
        ConnectCallback = ConnectAsync,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    };

    /// <inheritdoc />
    public async Task<HttpResponseMessage> DownloadAsync(Uri url, FileDownloadOptions? options = default, CancellationToken cancellationToken = default)
    {
        if (!url.IsAbsoluteUri || url.Scheme != Uri.UriSchemeHttps || !url.IsDefaultPort ||
            url.HostNameType != UriHostNameType.Dns || url.UserInfo.Length != 0 ||
            url.Fragment.Length != 0 || !(_options.Value.RemoteDownloadHosts ?? []).Contains(url.IdnHost, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("The remote download destination is not allowed.");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (options?.ETag != null)
            request.Headers.IfNoneMatch.Add(options.ETag);

        if (options?.Range != null)
            request.Headers.Range = options.Range;

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if ((int)response.StatusCode is >= 300 and < 400 and not 304)
        {
            response.Dispose();
            throw new InvalidOperationException("Remote download redirects are not allowed.");
        }

        return response;
    }

    private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint;
        // Use validated addresses directly so the socket cannot re-resolve the host after this check.
        var addresses = await Dns.GetHostAddressesAsync(host.Host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(address => !IsPublicAddress(address)))
            throw new InvalidOperationException("The remote download destination is not allowed.");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(addresses, host.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] is not (0 or 10 or 127 or >= 224) &&
                   !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127) &&
                   !(bytes[0] == 169 && bytes[1] == 254) &&
                   !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
                   !(bytes[0] == 192 && (bytes[1] is 0 or 168 || bytes[1] == 88 && bytes[2] == 99)) &&
                   !(bytes[0] == 198 && (bytes[1] is 18 or 19 || bytes[1] == 51 && bytes[2] == 100)) &&
                   !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113);
        }

        return address.AddressFamily == AddressFamily.InterNetworkV6 &&
               (bytes[0] & 0xe0) == 0x20 &&
               !(bytes[0] == 0x20 && (bytes[1] == 0x02 || bytes[1] == 0x01 && (bytes[2] < 0x02 || bytes[2] == 0x0d && bytes[3] == 0xb8))) &&
               !(bytes[0] == 0x3f && (bytes[1] & 0xf0) == 0xf0);
    }
}
