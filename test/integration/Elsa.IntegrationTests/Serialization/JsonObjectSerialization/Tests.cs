using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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

    [Fact(DisplayName = "Serialize and deserialize JsonObject create island")]
    public void Test_JsonObject_Serialization_create_Island()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var jsonContent = "{\"premiumCalculationAfdToIipEngine\":{\"script\":[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]} }";

        var dict = new Dictionary<string, object>
        {
            { "StatusCode", "Created" },
            { "Content", JsonObject.Parse(jsonContent) }
        };

        var result = payloadSerializer.Serialize(dict);

        var expected = File.ReadAllText("Serialization/JsonObjectSerialization/JsonObjectIsland.json");
      
        var jsonResult = JsonObject.Parse(result).ToString();
        var jsonExpected = JsonObject.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }

    [Fact(DisplayName = "Serialize and deserialize JsonObject read island")]
    public void Test_JsonObject_Serialization_read_Island()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var jsonContent = File.ReadAllText("Serialization/JsonObjectSerialization/JsonObjectIsland.json");

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonContent);

        var result = JsonObject.Create(JsonSerializer.SerializeToElement(transformationModel));

        var expected = File.ReadAllText("Serialization/JsonObjectSerialization/JsonObjectWithoutType.json");

        var jsonResult = result.ToString();
        var jsonExpected = JsonObject.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }

    [Fact(DisplayName = "Serialize and deserialize JsonObject")]
    public void Test_JsonObject_Serialization_roundtrip()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var jsonContent = "{\"premiumCalculationAfdToIipEngine\":{\"script\":[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]} }";

        var dict = new Dictionary<string, object>
        {
            { "StatusCode", "Created" },
            { "Content", JsonObject.Parse(jsonContent) }
        };

        var jsonSerialized = payloadSerializer.Serialize(dict);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonSerialized);

        var result = transformationModel["Content"].ToString();
        var expected = "{\"premiumCalculationAfdToIipEngine\":{\"script\":[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]} }";

        var jsonResult = JsonObject.Parse(result).ToString();
        var jsonExpected = JsonObject.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }


    [Fact(DisplayName = "Serialize and deserialize JObject create island")]
    public void Test_JObject_Serialization_create_Island()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var jsonContent = "{\"premiumCalculationAfdToIipEngine\":{\"script\":[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]} }";

        Dictionary<string, object> dict = new Dictionary<string, object>
        {
            { "StatusCode", "Created" },
            { "Content", JObject.Parse(jsonContent) }
        };

        var result = payloadSerializer.Serialize(dict);

        var expected = File.ReadAllText("Serialization/JsonObjectSerialization/JObjectIsland.json");

        var jsonResult = JsonObject.Parse(result).ToString();
        var jsonExpected = JsonObject.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }

    [Fact(DisplayName = "Serialize and deserialize JObject read island")]
    public void Test_JObject_Serialization_read_Island()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var jsonContent = File.ReadAllText("Serialization/JsonObjectSerialization/JObjectIsland.json");

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonContent);

        var result =  Newtonsoft.Json.JsonConvert.SerializeObject(transformationModel);

        var expected = File.ReadAllText("Serialization/JsonObjectSerialization/JObjectWithoutType.json");

        var jsonResult = JObject.Parse(result).ToString();
        var jsonExpected = JObject.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }

    [Fact(DisplayName = "Serialize and deserialize JObject")]
    public void Test_JObject_Serialization_roundtrip()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var jsonContent = "{\"premiumCalculationAfdToIipEngine\":{\"script\":[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]} }";

        var dict = new Dictionary<string, object>
        {
            { "StatusCode", "Created" },
            { "Content", JObject.Parse(jsonContent) }
        };

        var jsonSerialized = payloadSerializer.Serialize(dict);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonSerialized);

        var result = transformationModel["Content"].ToString();
        var expected = "{\"premiumCalculationAfdToIipEngine\":{\"script\":[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]} }";

        var jsonResult = JObject.Parse(result).ToString();
        var jsonExpected = JObject.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }


    [Fact(DisplayName = "Serialize and deserialize JArray create island")]
    public void Test_JArray_Serialization_create_Island()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var jsonContent = "[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]";

        var dict = new Dictionary<string, object>
        {
            { "StatusCode", "Created" },
            { "Array", JArray.Parse(jsonContent) }
        };

        var result = payloadSerializer.Serialize(dict);

        var expected = File.ReadAllText("Serialization/JsonObjectSerialization/JArrayIsland.json");

        var jsonResult = JsonObject.Parse(result).ToString();
        var jsonExpected = JsonObject.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }

    [Fact(DisplayName = "Serialize and deserialize JArray read island")]
    public void Test_JArray_Serialization_read_Island()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var jsonContent = File.ReadAllText("Serialization/JsonObjectSerialization/JArrayIsland.json");

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonContent);

        var result = Newtonsoft.Json.JsonConvert.SerializeObject(transformationModel);

        var expected = File.ReadAllText("Serialization/JsonObjectSerialization/JArrayWithoutType.json");

        var jsonResult = JObject.Parse(result).ToString();
        var jsonExpected = JObject.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }

    [Fact(DisplayName = "Serialize and deserialize JArray")]
    public void Test_JArray_Serialization_roundtrip()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var jsonContent = "[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]";

        var dict = new Dictionary<string, object>
        {
            { "StatusCode", "Created" },
            { "Array", JArray.Parse(jsonContent) }
        };

        var jsonSerialized = payloadSerializer.Serialize(dict);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonSerialized);

        var result = transformationModel["Array"].ToString();
        var expected = "[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]";

        var jsonResult = JArray.Parse(result).ToString();
        var jsonExpected = JArray.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }


    [Fact(DisplayName = "Serialize and deserialize JsonArray create island")]
    public void Test_JsonArray_Serialization_create_Island()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var jsonContent = "[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]";

        var dict = new Dictionary<string, object>
        {
            { "StatusCode", "Created" },
            { "Array", JsonArray.Parse(jsonContent) }
        };

        var result = payloadSerializer.Serialize(dict);

        var expected = File.ReadAllText("Serialization/JsonObjectSerialization/JsonArrayIsland.json");

        var jsonResult = JsonObject.Parse(result).ToString();
        var jsonExpected = JsonObject.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }

    [Fact(DisplayName = "Serialize and deserialize JsonArray read island")]
    public void Test_JsonArray_Serialization_read_Island()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var jsonContent = File.ReadAllText("Serialization/JsonObjectSerialization/JsonArrayIsland.json");

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonContent);

        var result = JsonArray.Create(JsonSerializer.SerializeToElement(transformationModel["Array"])).ToString();

        var expected = File.ReadAllText("Serialization/JsonObjectSerialization/JsonArrayWithoutType.json");

        var jsonResult = JsonArray.Parse(result).ToString();
        var jsonExpected = JsonArray.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }

    [Fact(DisplayName = "Serialize and deserialize JsonArray")]
    public void Test_JsonArray_Serialization_roundtrip()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var jsonContent = "[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]";

        var dict = new Dictionary<string, object>
        {
            { "StatusCode", "Created" },
            { "Array", JsonArray.Parse(jsonContent) }
        };

        var jsonSerialized = payloadSerializer.Serialize(dict);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonSerialized);

        var result = transformationModel["Array"].ToString();
        var expected = "[{\"path\":\"$.INPUT_CALCS.newCalculation\",\"command\":\"add\"}]";

        var jsonResult = JsonArray.Parse(result).ToString();
        var jsonExpected = JsonArray.Parse(expected).ToString();

        Assert.Equal(jsonExpected, jsonResult);
    }
}



