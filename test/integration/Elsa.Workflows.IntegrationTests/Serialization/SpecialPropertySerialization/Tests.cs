using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Nodes;
using Elsa.Workflows.Contracts;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Serialization.SpecialPropertySerialization
{
    public class Tests
    {
        private readonly IServiceProvider _services;

        public Tests(ITestOutputHelper testOutputHelper)
        {
            _services = new TestApplicationBuilder(testOutputHelper).Build();
        }

        [Fact(DisplayName = "Serialize and deserialize properties with $ prefix")]
        public void Test_Special_Properties_Serialization_roundtrip()
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

            Assert.Equal(jsonExpected, jsonResult);
        }
    }
}