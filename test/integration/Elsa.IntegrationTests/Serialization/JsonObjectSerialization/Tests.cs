using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elastic.Transport;
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

        Assert.Equal("Created", transformationModel["Content"]);
    }

    [Fact(DisplayName = "Serialize and deserialize JObject")]
    public void Test_JObject_Serialization()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var transformationJson = File.ReadAllText("Serialization/JsonObjectSerialization/JObject.json");

        var jsonObject = JObject.Parse(transformationJson);

        var jsonString = payloadSerializer.Serialize(transformationJson);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonString);

        Assert.Equal("Created", transformationModel["Content"]);
    }

    [Fact(DisplayName = "Serialize and deserialize JArray")]
    public void Test_JArray_Serialization()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var transformationJson = File.ReadAllText("Serialization/JsonObjectSerialization/JArray.json");

        var jsonObject = JArray.Parse(transformationJson);

        var jsonString = payloadSerializer.Serialize(jsonObject);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonString);

        Assert.Equal("Created", transformationModel["Content"]);
    }


    [Fact(DisplayName = "Serialize and deserialize JsonArray")]
    public void Test_JsonArray_Serialization()
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();

        var transformationJson = File.ReadAllText("Serialization/JsonObjectSerialization/JsonArray.json");

        var jsonObject = JsonArray.Parse(transformationJson);

        var jsonString = payloadSerializer.Serialize(jsonObject);

        var transformationModel = payloadSerializer.Deserialize<IDictionary<string, object>>(jsonString);

        var result = transformationModel["Array"].ToString();
        var expected = "{\r\n    \"_items\": [\r\n      {\r\n        \"path\": \"$.INPUT_CALCS.newCalculation\",\r\n        \"description\": \"default empty values when CALC_NEW \",\r\n        \"value\": {\r\n          \"MP_COVERAGE_LAYER_ID\": \"101001\",\r\n          \"MP_MAIN_YN\": \"TRUE\",\r\n          \"MP_STATUS_KEY\": \"STATUS_prm\",\r\n          \"MP_PREMIUM_END_DT\": \"9999-12-31\",\r\n          \"MP_AGE_CORR_FISCAL_1_YRS\": 0,\r\n          \"MP_AGE_CORR_FISCAL_2_YRS\": null,\r\n          \"MP_BENEFITS\": 7000\r\n        },\r\n        \"command\": \"add\"\r\n      }\r\n    ],\r\n    \"_type\": \"System.Text.Json.Nodes.JsonArray, System.Text.Json\"\r\n  }";


        Assert.Equal(expected, result);
    }

}



