using System.Net;
using System.Security.Cryptography;
using System.Text;
using Elsa.Extensions;
using Elsa.Http.Webhooks.Features;
using Elsa.Http.Webhooks.Notifications;
using Elsa.Mediator.Contracts;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using WebhooksCore;

namespace Elsa.Http.IntegrationTests.Webhooks;

public class WebhookIngressTests
{
    private static readonly byte[] SigningKey = SHA256.HashData("test webhook source key"u8);
    private static readonly byte[] OtherKey = SHA256.HashData("another test webhook source key"u8);
    private const string Event = """{"eventType":"test-event","payload":{}}""";

    [Fact]
    public async Task DispatchRequiresValidFreshSignatureAndOnlyOneDeliveryIsAccepted()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(WebhooksFeature).Assembly];
            options.DisableAutoDiscovery = true;
        });
        builder.Services.AddElsa(elsa => elsa
            .UseWorkflowManagement()
            .UseWorkflowRuntime()
            .UseWorkflows()
            .UseWebhooks(feature =>
            {
                feature.ConfigureIngress = options =>
                {
                    options.SigningKeys["integration"] = Convert.ToBase64String(SigningKey);
                    options.SigningKeys["other-source"] = Convert.ToBase64String(OtherKey);
                };
                feature.RegisterWebhookSources(
                    new WebhookSource
                    {
                        Id = "integration",
                        Name = "Integration",
                        EventTypes =
                        [
                            new WebhookSourceEventType { EventType = "test-event" },
                            new WebhookSourceEventType { EventType = "colliding-event" }
                        ]
                    },
                    new WebhookSource
                    {
                        Id = "other-source",
                        Name = "Other source",
                        EventTypes = [new WebhookSourceEventType { EventType = "other-event" }]
                    },
                    new WebhookSource
                    {
                        Id = "conflicting-source",
                        Name = "Integration",
                        EventTypes = [new WebhookSourceEventType { EventType = "colliding-event" }]
                    });
            }));
        var witness = new NotificationWitness();
        builder.Services.AddNotificationHandler<NotificationWitness, WebhookEventReceived>(_ => witness);

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseFastEndpoints();
        await app.StartAsync();
        using var client = app.GetTestClient();

        using var unsigned = await client.PostAsync("/webhooks", new StringContent(Event, Encoding.UTF8, "application/json"));
        using var wrongSource = await SendSignedAsync(client, "integration", "delivery", Event, OtherKey);
        using var stale = await SendSignedAsync(client, "integration", "delivery", Event, SigningKey, DateTimeOffset.UtcNow.AddMinutes(-10));
        using var wrongEvent = await SendSignedAsync(client, "other-source", "delivery", Event, OtherKey);

        using var ambiguous = await SendSignedAsync(client, "integration", "delivery",
            """{"eventType":"colliding-event","payload":{}}""", SigningKey);

        Assert.Equal(HttpStatusCode.Unauthorized, unsigned.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongSource.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, stale.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, wrongEvent.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, ambiguous.StatusCode);
        Assert.Equal(0, witness.Deliveries);

        var duplicates = await Task.WhenAll(
            SendSignedAsync(client, "integration", "delivery", Event, SigningKey),
            SendSignedAsync(client, "integration", "delivery", Event, SigningKey));
        Assert.Single(duplicates, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(duplicates, response => response.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, witness.Deliveries);
        foreach (var response in duplicates)
            response.Dispose();
    }

    private static async Task<HttpResponseMessage> SendSignedAsync(
        HttpClient client, string source, string delivery, string body, byte[] key, DateTimeOffset? time = null)
    {
        var timestamp = (time ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var signature = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{source}\n{delivery}\n{timestamp}\n{body}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Elsa-Webhook-Source", source);
        request.Headers.Add("X-Elsa-Webhook-Delivery", delivery);
        request.Headers.Add("X-Elsa-Webhook-Timestamp", timestamp);
        request.Headers.Add("X-Elsa-Webhook-Signature", Convert.ToBase64String(signature));
        return await client.SendAsync(request);
    }

    private sealed class NotificationWitness : INotificationHandler<WebhookEventReceived>
    {
        private int _deliveries;
        public int Deliveries => Interlocked.CompareExchange(ref _deliveries, 0, 0);

        public Task HandleAsync(WebhookEventReceived notification, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _deliveries);
            return Task.CompletedTask;
        }
    }
}
