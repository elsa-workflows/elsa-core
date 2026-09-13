using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Nodes;

namespace Elsa.Workflows.IntegrationTests.Serialization.SpecialPropertySerialization
{
    public class Tests : IAsyncDisposable
    {
        private readonly IServiceProvider _services;

        public Tests()
        {
            _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).Build();
        }

        [Test]
        [DisplayName("Serialize and deserialize properties with $ prefix")]
        public async Task Test_Special_Properties_Serialization_roundtrip()
        {
            var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();
            var jsonContent = "{\"prop1\":{\"script\":[{\"$id\":\"someid\"}]} }";

            var dict = new Dictionary<string, object>
            {
                { "Content", JsonNode.Parse(jsonContent)! }
            };

            var jsonSerialized = payloadSerializer.Serialize(dict);
            var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonSerialized);
            var result = transformationModel["Content"].ToString()!;
            var expected = "{\"prop1\":{\"script\":[{\"$id\":\"someid\"}]} }";
            var jsonResult = JsonNode.Parse(result)?.ToString();
            var jsonExpected = JsonNode.Parse(expected)?.ToString();

            await Assert.That(jsonResult).IsEqualTo(jsonExpected);
        }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
}
