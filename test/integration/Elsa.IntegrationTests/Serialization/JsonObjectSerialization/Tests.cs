using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Serialization.JsonObjectSerialization;

public class Tests
{
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).Build();
    }

    [Fact(DisplayName = "Serialize and deserialize JsonObject")]
    public void Test_JsonObject_Serialization()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var transformationJson = File.ReadAllText("Serialization/JsonObjectSerialization/JsonObject.json");

        var jsonObject = JsonObject.Parse(transformationJson);

        var jsonString = payloadSerializer.Serialize(jsonObject);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string,object>>(jsonString);

        var result = transformationModel["Content"].ToString();
        var expected = "{\"premiumCalculationAfdToIipEngine\":{\"script\":[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]},\"_type\":\"System.Text.Json.Nodes.JsonObject, System.Text.Json\"  }";

        var jsonResult = JsonArray.Parse(result).ToString();
        var jsonExpected = JsonArray.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }

    [Fact(DisplayName = "Serialize and deserialize JObject")]
    public void Test_JObject_Serialization()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var transformationJson = File.ReadAllText("Serialization/JsonObjectSerialization/JObject.json");

        var Jobject = JObject.Parse(transformationJson);

        var jsonString = payloadSerializer.Serialize(Jobject);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonString);

        Assert.Equal("Created", transformationModel["Content"]);
    }

    [Fact(DisplayName = "Serialize and deserialize JArray")]
    public void Test_JArray_Serialization()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var transformationJson = File.ReadAllText("Serialization/JsonObjectSerialization/JArray.json");

        var jArray = JArray.Parse(transformationJson);

        var jsonString = payloadSerializer.Serialize(jArray);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonString);

        var result = transformationModel["Array"].ToString();
        var expected = "{\"_items\":[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}],\"_type\":\"System.Text.Json.Nodes.JsonArray, System.Text.Json\"  }";

        var jsonResult = JsonArray.Parse(result).ToString();
        var jsonExpected = JsonArray.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }


    [Fact(DisplayName = "Serialize and deserialize JsonArray")]
    public void Test_JsonArray_Serialization()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var transformationJson = File.ReadAllText("Serialization/JsonObjectSerialization/JsonArray.json");

        var jsonArray = JsonArray.Parse(transformationJson);

        var jsonString = payloadSerializer.Serialize(jsonArray);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonString);

        var result = transformationModel["Array"].ToString();
        var expected = "{\"_items\":[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}],\"_type\":\"System.Text.Json.Nodes.JsonArray, System.Text.Json\"  }";

        var jsonResult = JsonArray.Parse(result).ToString();
        var jsonExpected = JsonArray.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }

}



