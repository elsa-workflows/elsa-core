using System.Net.Http.Json;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Services;

namespace Elsa.Webhooks.Implementations;

/// <summary>
/// An implementation of <see cref="IWebhookInvoker"/> that uses a named <see cref="HttpClient"/>.
/// </summary>
public class HttpWebhookInvoker : IWebhookInvoker
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor.
    /// </summary>
    public HttpWebhookInvoker(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    /// <inheritdoc />
    public async Task InvokeWebhookAsync(WebhookRegistration registration, WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var url = registration.Endpoint;
        await _httpClient.PostAsJsonAsync(url, webhookEvent, cancellationToken);
    }
}