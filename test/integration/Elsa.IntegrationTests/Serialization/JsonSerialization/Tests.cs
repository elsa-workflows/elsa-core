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

namespace Elsa.IntegrationTests.Serialization.JsonSerialization;

public class SerializationTests
{
    private readonly IServiceProvider _services;

    public SerializationTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).Build();
    }

    [Theory(DisplayName = "write")]
    [InlineData(typeof(JsonObject), "JsonObjectIsland")]
    [InlineData(typeof(JObject), "JObjectIsland")]
    [InlineData(typeof(JsonArray), "JsonArrayIsland")]
    [InlineData(typeof(JArray), "JArrayIsland")]
    public void Test_Serialization_create_Island(Type type, string fileName)
    {
        var dict = GetContent(type);

        var result = SerializeUsingPayloadSerializer(dict);

        var expected = File.ReadAllText($@"Serialization/JsonSerialization/{fileName}.json");

        CompareJsonsObjects(expected, result);
    }

    [Theory(DisplayName = "roundtrip")]
    [InlineData(typeof(JsonObject))]
    [InlineData(typeof(JObject))]
    [InlineData(typeof(JsonArray))]
    [InlineData(typeof(JArray))]
    public void Test_Serialization_roundtrip(Type type)
    {
        var dict = GetContent(type);
        var jsonSerialized = SerializeUsingPayloadSerializer(dict);
        var transformationModel = DeSerializeDictionaryUsingPayloadSerializer(jsonSerialized);
        var result = transformationModel["Content"].ToString();
        var expected = GetExpected(type);

        CompareJsonsObjects(expected, result);
    }

    [Theory(DisplayName = "read")]
    [InlineData(typeof(JsonObject), "JsonObjectIsland", "JsonObjectWithoutType")]
    [InlineData(typeof(JObject), "JObjectIsland", "JObjectWithoutType")]
    [InlineData(typeof(JsonArray), "JsonArrayIsland", "JsonArrayWithoutType")]
    [InlineData(typeof(JArray), "JArrayIsland", "JArrayWithoutType")]
    public void Test_Serialization_read(Type type, string fileName, string compareFileName)
    {
        var jsonContent = File.ReadAllText(@$"Serialization/JsonSerialization/{fileName}.json");

        var transformationModel = DeSerializeDictionaryUsingPayloadSerializer(jsonContent);

        var result = "";
        if (type == typeof(JObject) || type == typeof(JArray))
        {
            result = Newtonsoft.Json.JsonConvert.SerializeObject(transformationModel);
        }
        else
        {
            result = type == typeof(JsonObject) ? JsonObject.Create(JsonSerializer.SerializeToElement(transformationModel)).ToString() : JsonArray.Create(JsonSerializer.SerializeToElement(transformationModel["Content"])).ToString();
        }

        var expected = File.ReadAllText(@$"Serialization/JsonSerialization/{compareFileName}.json");

        CompareJsonsObjects(expected, result);
    }

    private string SerializeUsingPayloadSerializer(object obj)
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();
        return payloadSerializer.Serialize(obj);
    }

    private IDictionary<string, object> DeSerializeDictionaryUsingPayloadSerializer(string jsonString)
    {
        var payloadSerializer = _services.GetRequiredService<IPayloadSerializer>();
        return payloadSerializer.Deserialize<IDictionary<string, object>>(jsonString);
    }

    private void CompareJsonsObjects(string expected, string actual)
    {
        var jsonActual = NormalizeNewlines(JsonNode.Parse(actual!)?.ToString());
        var jsonExpected = NormalizeNewlines(JsonNode.Parse(expected!)?.ToString());

        Assert.Equal(jsonExpected, jsonActual);
    }

    private IDictionary<string, object> GetContent(Type type)
    {
        var isArray = type == typeof(JsonArray) || type == typeof(JArray);

        var jsonContent = isArray ? "[{\"path\":\"folder1\",\"command\":\"add\"}]": "{\"file1\":{\"script\":[{\"path\":\"folder1\",\"command\":\"add\"}]} }";

        var dict = new Dictionary<string, object>
        {
            { "StatusCode", "Created" },
            { "Content",isArray ? 
            (type == typeof(JArray)? JArray.Parse(jsonContent):JsonArray.Parse(jsonContent)):
            (type == typeof(JObject)? JObject.Parse(jsonContent):JsonObject.Parse(jsonContent))
            }
        };
        return dict;
    }

    private string GetExpected(Type type)
    {
        if (type == typeof(JsonArray) || type == typeof(JArray)) {
            return "[{\"path\":\"folder1\",\"command\":\"add\"}]";
        }
        else
        {
            return "{\"file1\":{\"script\":[{\"path\":\"folder1\",\"command\":\"add\"}]} }";
        }
    }

    private static string? NormalizeNewlines(string? input) => input?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
}