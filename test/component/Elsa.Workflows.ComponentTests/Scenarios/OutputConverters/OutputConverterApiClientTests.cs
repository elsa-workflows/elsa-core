using System.Net;
using System.Text;
using Elsa.Api.Client;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.OutputConverters.Contracts;
using Elsa.Api.Client.Resources.OutputConverters.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Refit;

namespace Elsa.Workflows.ComponentTests.Scenarios.OutputConverters;

public class OutputConverterApiClientTests
{
    [Fact]
    public void AddDefaultApiClients_RegistersOutputConverterDiscoveryClient()
    {
        var services = new ServiceCollection();
        services.AddDefaultApiClients(options => options.BaseAddress = new Uri("https://example.test/elsa/api"));

        using var serviceProvider = services.BuildServiceProvider();

        Assert.IsAssignableFrom<IOutputConvertersApi>(serviceProvider.GetRequiredService<IOutputConvertersApi>());
    }

    [Fact]
    public async Task ListAsync_SendsDeclaredTypesAndDeserializesTheSafeDescriptorShape()
    {
        var handler = new ResponseHandler("""
            {
              "items": [
                {
                  "id": "sample.to-text",
                  "sourceTypeName": "String",
                  "resultTypeName": "String",
                  "displayName": "Convert to text",
                  "description": "Formats the source as text.",
                  "settingsSchema": { "type": "object" }
                }
              ]
            }
            """);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/elsa/api") };
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var api = RestService.For<IOutputConvertersApi>(client, RefitSettingsHelper.CreateRefitSettings(serviceProvider));

        var response = await api.ListAsync(new ListOutputConvertersRequest
        {
            SourceType = "String",
            DestinationType = "String"
        });

        Assert.Equal("/elsa/api/descriptors/output-converters?sourceType=String&destinationType=String", handler.RequestUri!.PathAndQuery);
        var descriptor = Assert.Single(response.Items);
        Assert.Equal("sample.to-text", descriptor.Id);
        Assert.Equal("String", descriptor.SourceTypeName);
        Assert.Equal("String", descriptor.ResultTypeName);
        Assert.Equal("object", descriptor.SettingsSchema!.Value.GetProperty("type").GetString());
    }

    [Fact]
    public async Task AddApiClient_UsesConfiguredResilienceRetryHandler()
    {
        var handler = new RetryThenSucceedHandler("""
            {
              "items": [
                {
                  "id": "sample.to-text",
                  "sourceTypeName": "String",
                  "resultTypeName": "String",
                  "displayName": "Convert to text"
                }
              ]
            }
            """, failuresBeforeSuccess: 2);
        var services = new ServiceCollection();

        services.AddApiClient<IOutputConvertersApi>(options =>
        {
            options.BaseAddress = new Uri("https://example.test/elsa/api");
            options.ConfigureHttpClientBuilder = builder => builder.ConfigurePrimaryHttpMessageHandler(() => handler);
            options.ConfigureRetryPolicy = builder => builder.AddResilienceHandler("test-retry", pipeline => pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.Zero,
                BackoffType = DelayBackoffType.Constant,
                UseJitter = false,
                ShouldRetryAfterHeader = false
            }));
        });

        using var serviceProvider = services.BuildServiceProvider();
        var api = serviceProvider.GetRequiredService<IOutputConvertersApi>();

        var response = await api.ListAsync(new ListOutputConvertersRequest
        {
            SourceType = "String",
            DestinationType = "String"
        });

        Assert.Equal(3, handler.RequestCount);
        Assert.Single(response.Items);
    }

    private sealed class ResponseHandler(string response) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class RetryThenSucceedHandler(string response, int failuresBeforeSuccess) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;

            if (RequestCount <= failuresBeforeSuccess)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
        }
    }
}
