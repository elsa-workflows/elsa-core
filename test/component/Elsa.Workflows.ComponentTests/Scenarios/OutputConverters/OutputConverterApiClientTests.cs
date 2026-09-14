using System.Net;
using System.Text;
using Elsa.Api.Client;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.OutputConverters.Contracts;
using Elsa.Api.Client.Resources.OutputConverters.Requests;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Elsa.Workflows.ComponentTests.Scenarios.OutputConverters;

public class OutputConverterApiClientTests
{
    [Test]
    public async Task AddDefaultApiClients_RegistersOutputConverterDiscoveryClient()
    {
        var services = new ServiceCollection();
        services.AddDefaultApiClients(options => options.BaseAddress = new Uri("https://example.test/elsa/api"));

        using var serviceProvider = services.BuildServiceProvider();

        await Assert.That(serviceProvider.GetRequiredService<IOutputConvertersApi>()).IsAssignableTo<IOutputConvertersApi>();
    }

    [Test]
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

        await Assert.That(handler.RequestUri!.PathAndQuery).IsEqualTo("/elsa/api/descriptors/output-converters?sourceType=String&destinationType=String");
        var descriptor = await Assert.That(response.Items).HasSingleItem();
        await Assert.That(descriptor.Id).IsEqualTo("sample.to-text");
        await Assert.That(descriptor.SourceTypeName).IsEqualTo("String");
        await Assert.That(descriptor.ResultTypeName).IsEqualTo("String");
        await Assert.That(descriptor.SettingsSchema!.Value.GetProperty("type").GetString()).IsEqualTo("object");
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
}