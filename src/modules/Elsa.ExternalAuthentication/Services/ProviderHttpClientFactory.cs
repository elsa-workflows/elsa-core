using System.Net;
using System.Net.Http;
using System.Text;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Validation;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Creates the protocol-neutral HTTP client used for all provider traffic.
/// Redirects are deliberately handled by <see cref="ProviderHttpClient"/> so every hop is revalidated.
/// </summary>
public sealed class ProviderHttpClientFactory : IProviderHttpClientFactory, IDisposable
{
    private readonly HttpMessageInvoker invoker;
    private readonly OutboundDestinationValidator destinationValidator;
    private readonly IOptions<ExternalAuthenticationOptions> options;

    public ProviderHttpClientFactory(
        IOptions<ExternalAuthenticationOptions> options,
        OutboundDestinationValidator destinationValidator,
        ValidatedOutboundConnectionFactory connectionFactory)
    {
        this.options = options;
        this.destinationValidator = destinationValidator;
        UsesApprovedProxy = options.Value.ProviderEgress.ProxyUri is not null;
        if (options.Value.ProviderEgress.ProxyUri is { } proxyUri)
            destinationValidator.ValidateApprovedProxy(proxyUri);

        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectTimeout = options.Value.ProviderEgress.ConnectTimeout,
            UseProxy = UsesApprovedProxy
        };

        if (UsesApprovedProxy)
            handler.Proxy = new WebProxy(options.Value.ProviderEgress.ProxyUri!);
        else
            handler.ConnectCallback = (context, cancellationToken) => connectionFactory.ConnectAsync(context.DnsEndPoint, cancellationToken);

        invoker = new HttpMessageInvoker(handler, disposeHandler: true);
    }

    /// <summary>
    /// A configured proxy is deployment-owned and the sole explicitly approved egress gateway. Requested destinations are still validated before proxying.
    /// </summary>
    public bool UsesApprovedProxy { get; }

    public IProviderHttpClient CreateClient() => new ProviderHttpClient(invoker, destinationValidator, options);

    public void Dispose() => invoker.Dispose();
}

public interface IProviderHttpClientFactory
{
    IProviderHttpClient CreateClient();
}

public interface IProviderHttpClient
{
    ValueTask<ProviderHttpResponse> GetAsync(Uri uri, ProviderResponseKind kind, CancellationToken cancellationToken = default);
    ValueTask<ProviderHttpResponse> PostFormAsync(Uri uri, IReadOnlyDictionary<string, string> values, ProviderResponseKind kind, CancellationToken cancellationToken = default);
}

public sealed class ProviderHttpClient(HttpMessageInvoker invoker, OutboundDestinationValidator destinationValidator, IOptions<ExternalAuthenticationOptions> options) : IProviderHttpClient
{
    public ValueTask<ProviderHttpResponse> GetAsync(Uri uri, ProviderResponseKind kind, CancellationToken cancellationToken = default) => SendAsync(uri, kind, address => new HttpRequestMessage(HttpMethod.Get, address), cancellationToken);

    public ValueTask<ProviderHttpResponse> PostFormAsync(Uri uri, IReadOnlyDictionary<string, string> values, ProviderResponseKind kind, CancellationToken cancellationToken = default) =>
        SendAsync(uri, kind, address => new HttpRequestMessage(HttpMethod.Post, address) { Content = new FormUrlEncodedContent(values) }, cancellationToken);

    private async ValueTask<ProviderHttpResponse> SendAsync(Uri uri, ProviderResponseKind kind, Func<Uri, HttpRequestMessage> createRequest, CancellationToken cancellationToken)
    {
        var redirects = 0;
        var current = uri;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.ProviderEgress.RequestTimeout);

        try
        {
            while (true)
            {
                await destinationValidator.ValidateAsync(current, timeout.Token);
                using var request = createRequest(current);
                using var response = await invoker.SendAsync(request, timeout.Token);

                if (IsRedirect(response.StatusCode))
                {
                    if (kind is ProviderResponseKind.Token or ProviderResponseKind.UserInfo || response.Headers.Location is null || redirects++ >= options.Value.ProviderEgress.MaximumRedirects)
                        throw new ProviderHttpException(ProviderHttpFailure.RedirectRejected);

                    current = new Uri(current, response.Headers.Location);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                    return new ProviderHttpResponse(response.StatusCode, []);

                return new ProviderHttpResponse(response.StatusCode, await ReadResponseBodyAsync(response, kind, timeout.Token));
            }
        }
        catch (OutboundDestinationException)
        {
            throw new ProviderHttpException(ProviderHttpFailure.DestinationRejected);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ProviderHttpException(ProviderHttpFailure.Timeout);
        }
        catch (ProviderHttpException)
        {
            throw;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ProviderHttpException(ProviderHttpFailure.TransportFailure);
        }
    }

    private async Task<byte[]> ReadResponseBodyAsync(HttpResponseMessage response, ProviderResponseKind kind, CancellationToken cancellationToken)
    {
        var limit = GetResponseLimit(kind);
        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength is not null && contentLength > limit)
            throw new ProviderHttpException(ProviderHttpFailure.ResponseTooLarge);

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                return output.ToArray();

            if (output.Length + read > limit)
                throw new ProviderHttpException(ProviderHttpFailure.ResponseTooLarge);

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private long GetResponseLimit(ProviderResponseKind kind) => kind switch
    {
        ProviderResponseKind.Token => options.Value.ProviderEgress.MaximumTokenResponseBytes,
        ProviderResponseKind.UserInfo => options.Value.ProviderEgress.MaximumUserInfoResponseBytes,
        _ => options.Value.ProviderEgress.MaximumDiscoveryResponseBytes
    };

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
}

public sealed record ProviderHttpResponse(HttpStatusCode StatusCode, byte[] Body)
{
    public bool IsSuccessStatusCode => (int)StatusCode is >= 200 and <= 299;
    public string ReadBodyAsUtf8() => Encoding.UTF8.GetString(Body);
}

public enum ProviderResponseKind
{
    Discovery,
    SigningKeys,
    Token,
    UserInfo
}

public enum ProviderHttpFailure
{
    DestinationRejected,
    RedirectRejected,
    Timeout,
    ResponseTooLarge,
    TransportFailure
}

public sealed class ProviderHttpException : InvalidOperationException
{
    public ProviderHttpException(ProviderHttpFailure failure) : base("The provider request could not be completed.")
    {
        Failure = failure;
    }

    public ProviderHttpFailure Failure { get; }
}
