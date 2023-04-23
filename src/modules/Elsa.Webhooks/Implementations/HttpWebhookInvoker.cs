using System.Net.Http.Json;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Webhooks.Implementations;

/// <summary>
/// An implementation of <see cref="IWebhookInvoker"/> that uses a named <see cref="HttpClient"/>.
/// </summary>
public class HttpWebhookInvoker : IWebhookInvoker
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public HttpWebhookInvoker(HttpClient httpClient, ILogger<HttpWebhookInvoker> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InvokeWebhookAsync(WebhookRegistration registration, WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var url = registration.Url;
        var response = await _httpClient.PostAsJsonAsync(url, webhookEvent, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Invoking webhook {Webhook} failed with status code {StatusCode} and content {Content}", registration.Url, response.StatusCode, content);
        }
    }
}