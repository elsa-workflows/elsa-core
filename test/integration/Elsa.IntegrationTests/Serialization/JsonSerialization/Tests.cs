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
    public void Test_Serialization_create_Island(Type type, string filename)
    {
        CreateIsland(type, filename);
    }

    [Theory(DisplayName = "roundtrip")]
    [InlineData(typeof(JsonObject))]
    [InlineData(typeof(JObject))]
    [InlineData(typeof(JsonArray))]
    [InlineData(typeof(JArray))]
    public void Test_Serialization_roundtrip(Type type)
    {
        TestRoundTrip(type);
    }

    [Theory(DisplayName = "read")]
    [InlineData(typeof(JsonObject), "JsonObjectIsland", "JsonObjectWithoutType")]
    [InlineData(typeof(JObject), "JObjectIsland", "JObjectWithoutType")]
    [InlineData(typeof(JsonArray), "JsonArrayIsland", "JsonArrayWithoutType")]
    [InlineData(typeof(JArray), "JArrayIsland", "JArrayWithoutType")]
    public void Test_Serialization_read(Type type, string filename, string compareFileName)
    {
        ReadIsland(type, filename, compareFileName);
    }

    private IDictionary<string, object> GetArrayContent(bool isJArray)
    {
        const string jsonContent = "[{\"path\":\"folder1\",\"command\":\"add\"}]";

        var dict = new Dictionary<string, object>
        {
            { "StatusCode", "Created" },
            { "Content", (isJArray ? JArray.Parse(jsonContent) : JsonNode.Parse(jsonContent))! }
        };
        return dict;
    }

    private static IDictionary<string, object> GetObjectContent(bool isJObject)
    {
        const string jsonContent = "{\"file1\":{\"script\":[{\"path\":\"folder1\",\"command\":\"add\"}]} }";

        var dict = new Dictionary<string, object>
        {
            { "StatusCode", "Created" },
            { "Content", (isJObject ? JObject.Parse(jsonContent) : JsonNode.Parse(jsonContent))! }
        };
        return dict;
    }

    private static string GetExpectedObject() => "{\"file1\":{\"script\":[{\"path\":\"folder1\",\"command\":\"add\"}]} }";
    private static string GetExpectedArray() => "[{\"path\":\"folder1\",\"command\":\"add\"}]";

    private void TestRoundTrip(Type type)
    {
        var dict = type == typeof(JsonArray) || type == typeof(JArray) ? GetArrayContent(type == typeof(JArray)) : GetObjectContent(type == typeof(JObject));
        var jsonSerialized = SerializeUsingPayloadSerializer(dict);
        var transformationModel = DeSerializeDictionaryUsingPayloadSerializer(jsonSerialized);
        var result = transformationModel["Content"].ToString();
        var expected = type == typeof(JsonArray) || type == typeof(JArray) ? GetExpectedArray() : GetExpectedObject();

        CompareJsonsObjects(expected, result);
    }

    private void CreateIsland(Type type, string fileName)
    {
        var dict = type == typeof(JsonArray) || type == typeof(JArray) ? GetArrayContent(type == typeof(JArray)) : GetObjectContent(type == typeof(JObject));
        var result = SerializeUsingPayloadSerializer(dict);
        var expected = File.ReadAllText($@"Serialization/JsonSerialization/{fileName}.json");

        CompareJsonsObjects(expected, result);
    }

    private void ReadIsland(Type type, string fileName, string compareFileName)
    {
        var jsonContent = File.ReadAllText(@$"Serialization/JsonSerialization/{fileName}.json");
        var transformationModel = DeSerializeDictionaryUsingPayloadSerializer(jsonContent);
        string? result;

        if (type == typeof(JObject) || type == typeof(JArray))
            result = Newtonsoft.Json.JsonConvert.SerializeObject(transformationModel);
        else
            result = type == typeof(JsonObject) ? JsonObject.Create(JsonSerializer.SerializeToElement(transformationModel))?.ToString() : JsonArray.Create(JsonSerializer.SerializeToElement(transformationModel["Content"]))?.ToString();

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

    private static void CompareJsonsObjects(string? expected, string? actual)
    {
        var jsonActual = NormalizeNewlines(JsonNode.Parse(actual!)?.ToString());
        var jsonExpected = NormalizeNewlines(JsonNode.Parse(expected!)?.ToString());

        Assert.Equal(jsonExpected, jsonActual);
    }

    private static string? NormalizeNewlines(string? input) => input?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");
}