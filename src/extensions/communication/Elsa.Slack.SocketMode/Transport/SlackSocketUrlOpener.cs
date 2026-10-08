using System.Text;
using System.Text.Json;
using Elsa.Slack.SocketMode.Events;
using Newtonsoft.Json;
using SlackNet;
using SlackNet.WebApi;

namespace Elsa.Slack.SocketMode.Transport;

/// <summary>Uses the pinned SDK API/deserializer with a bounded private HTTP transport and no automatic retries.</summary>
internal sealed class SlackSocketUrlOpener(SlackSocketModeConfiguration configuration, SlackSocketTransportPolicy policy)
{
    internal async Task<Uri> OpenAsync(string appToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(appToken))
        {
            throw new InvalidOperationException("socket_credential_unavailable");
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(configuration.Limits.OperationTimeout);
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
        using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        // A private settings instance uses only pinned SDK types and its explicit null logger.
        // Default SDK HTTP logs response bodies and is unbounded, so it is deliberately not used.
        var settings = Default.JsonSettings(Default.SlackTypeResolver(typeof(SlackApiClient).Assembly), Default.Logger);
        settings.SerializerSettings.TypeNameHandling = TypeNameHandling.None;
        settings.SerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
        settings.SerializerSettings.MaxDepth = configuration.Limits.MaximumJsonDepth;
        var http = new BoundedHttp(client, settings, policy, configuration.Limits);
        var sdk = new SlackApiClient(http, new FixedUrlBuilder(policy), settings, appToken)
        {
            DisableRetryOnRateLimit = true,
            WarningsAsErrors = true
        };
        try
        {
            var response = await sdk.AppsConnectionsApi.Open(timeout.Token);
            // SlackNet's converter can return a default after a conversion error. Never treat it as a valid URL.
            if (response?.Url is null || response.Url != http.ValidatedUrl || !Uri.TryCreate(response.Url, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException("socket_open_response_invalid");
            }
            policy.ValidateSocket(uri);
            return uri;
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new OperationCanceledException("Socket URL opening was cancelled.", cancellationToken);
        }
        catch (Exception)
        {
            // SDK/provider exceptions can carry response data or ticket-bearing URLs; do not retain their inner exception.
            throw new InvalidOperationException("socket_open_failed");
        }
    }

    private sealed class FixedUrlBuilder(SlackSocketTransportPolicy policy) : ISlackUrlBuilder
    {
        public string Url(string apiMethod, Dictionary<string, object> args)
        {
            if (apiMethod != "apps.connections.open" || args.Count != 0)
            {
                throw new InvalidOperationException("socket_api_method_denied");
            }
            return policy.ApiEndpoint.AbsoluteUri;
        }
    }

    private sealed class BoundedHttp(HttpClient client, SlackJsonSettings settings, SlackSocketTransportPolicy policy,
        SlackSocketModeLimits limits) : IHttp
    {
        internal string? ValidatedUrl { get; private set; }

        public async Task<T> Execute<T>(HttpRequestMessage requestMessage, CancellationToken cancellationToken = default)
        {
            using var request = requestMessage;
            if (typeof(T) != typeof(WebApiResponse) || request.Method != HttpMethod.Post || request.RequestUri != policy.ApiEndpoint ||
                request.Headers.Authorization?.Scheme != "Bearer" || string.IsNullOrEmpty(request.Headers.Authorization.Parameter))
            {
                throw new InvalidOperationException("socket_api_request_denied");
            }
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType != "application/json" ||
                response.Content.Headers.ContentLength > limits.MaximumEnvelopeBytes)
            {
                throw new InvalidOperationException("socket_api_response_denied");
            }
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var bytes = new MemoryStream();
            var buffer = new byte[Math.Min(4096, limits.MaximumEnvelopeBytes)];
            while (true)
            {
                var count = await stream.ReadAsync(buffer, cancellationToken);
                if (count == 0)
                {
                    break;
                }
                if (bytes.Length + count > limits.MaximumEnvelopeBytes)
                {
                    throw new InvalidOperationException("socket_api_response_too_large");
                }
                bytes.Write(buffer, 0, count);
            }
            var content = bytes.ToArray();
            using var json = SlackSocketJson.Parse(content, limits.MaximumEnvelopeBytes, limits.MaximumJsonDepth);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("url", out var url) || url.ValueKind != JsonValueKind.String ||
                !Uri.TryCreate(url.GetString(), UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException("socket_api_response_invalid");
            }
            policy.ValidateSocket(uri);
            ValidatedUrl = url.GetString();
            var result = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(content), settings.SerializerSettings);
            return result ?? throw new InvalidOperationException("socket_api_deserialization_failed");
        }
    }
}
