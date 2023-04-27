using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Serialization.SpecialPropertySerialization
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
                { "Content", JsonObject.Parse(jsonContent) }
            };

            var jsonSerialized = payloadSerializer.Serialize(dict);

            var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonSerialized);

            var result = transformationModel["Content"].ToString();
            var expected = "{\"prop1\":{\"script\":[{\"$id\":\"someid\"}]} }";

            var jsonResult = JsonObject.Parse(result).ToString();
            var jsonExpected = JsonObject.Parse(expected).ToString();

            Assert.Equal(jsonExpected, jsonResult);
        }
    }
}
