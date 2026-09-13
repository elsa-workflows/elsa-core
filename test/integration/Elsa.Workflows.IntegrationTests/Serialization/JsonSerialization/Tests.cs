using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Elsa.Workflows.IntegrationTests.Serialization.JsonSerialization;

public class SerializationTests : IAsyncDisposable
{
    private readonly IServiceProvider _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
        .ConfigureServices(services => services.Configure<SerializationTypeOptions>(options => options.AddTypeAlias<TestObject>()))
        .Build();

    [Test]
    [DisplayName("write: $type")]
    [Arguments(typeof(JsonObject), "JsonObjectIsland")]
    [Arguments(typeof(JObject), "JObjectIsland")]
    [Arguments(typeof(JsonArray), "JsonArrayIsland")]
    [Arguments(typeof(JArray), "JArrayIsland")]
    public async Task Test_Serialization_create_Island(Type type, string fileName)
    {
        var dict = GetContent(type);

        var result = SerializeUsingPayloadSerializer(dict);

        var expected = File.ReadAllText($"Serialization/JsonSerialization/{fileName}.json");

        await CompareJsonObjectsAsync(expected, result);
    }

    [Test]
    [DisplayName("roundtrip: $type")]
    [Arguments(typeof(JsonObject))]
    [Arguments(typeof(JObject))]
    [Arguments(typeof(JsonArray))]
    [Arguments(typeof(JArray))]
    public async Task Test_Serialization_roundtrip(Type type)
    {
        var dict = GetContent(type);
        var jsonSerialized = SerializeUsingPayloadSerializer(dict);
        var transformationModel = DeSerializeDictionaryUsingPayloadSerializer(jsonSerialized);
        var result = transformationModel["Content"].ToString()!;
        var expected = GetExpected(type);

        await CompareJsonObjectsAsync(expected, result);
    }

    [Test]
    [DisplayName("read: $type")]
    [Arguments(typeof(JsonObject), "JsonObjectIsland", "JsonObjectWithoutType")]
    [Arguments(typeof(JObject), "JObjectIsland", "JObjectWithoutType")]
    [Arguments(typeof(JsonArray), "JsonArrayIsland", "JsonArrayWithoutType")]
    [Arguments(typeof(JArray), "JArrayIsland", "JArrayWithoutType")]
    public async Task Test_Serialization_read(Type type, string fileName, string compareFileName)
    {
        var jsonContent = File.ReadAllText(@$"Serialization/JsonSerialization/{fileName}.json");

        var transformationModel = DeSerializeDictionaryUsingPayloadSerializer(jsonContent);

        string result;
        if (type == typeof(JObject) || type == typeof(JArray))
        {
            result = Newtonsoft.Json.JsonConvert.SerializeObject(transformationModel);
        }
        else
        {
            result = type == typeof(JsonObject) ? JsonObject.Create(JsonSerializer.SerializeToElement(transformationModel))!.ToString() : JsonArray.Create(JsonSerializer.SerializeToElement(transformationModel["Content"]))!.ToString();
        }

        var expected = File.ReadAllText(@$"Serialization/JsonSerialization/{compareFileName}.json");

        await CompareJsonObjectsAsync(expected, result);
    }

    [Test]
    public async Task RoundtripComplexEnumerableObject()
    {
        var dict = new Dictionary<string, object>
        {
            {
                "Content", new List<TestObject>()
                {
                    new()
                    {
                        Data = "Hello World"
                    }
                }
            }
        };
        var jsonSerialized = SerializeUsingPayloadSerializer(dict);
        var transformationModel = DeSerializeDictionaryUsingPayloadSerializer(jsonSerialized);
        var result = transformationModel["Content"];
        await Assert.That(result.GetType()).IsEqualTo(typeof(List<TestObject>));
    }

    [Test]
    public async Task RoundtripPrimitiveCollections()
    {
        var dict = new Dictionary<string, object>
        {
            {
                "Content", new List<Guid>
                {
                    Guid.NewGuid()
                }
            }
        };
        var jsonSerialized = SerializeUsingPayloadSerializer(dict);
        var transformationModel = DeSerializeDictionaryUsingPayloadSerializer(jsonSerialized);
        var result = transformationModel["Content"];
        await Assert.That(result.GetType()).IsEqualTo(typeof(List<Guid>));
    }

    [Test]
    public async Task RoundtripPrimitiveArrays()
    {
        var dict = new Dictionary<string, object>
        {
            {
                "Content", new[]
                {
                    Guid.NewGuid()
                }
            }
        };
        var jsonSerialized = SerializeUsingPayloadSerializer(dict);
        var transformationModel = DeSerializeDictionaryUsingPayloadSerializer(jsonSerialized);
        var result = transformationModel["Content"];
        await Assert.That(result.GetType()).IsEqualTo(typeof(Guid[]));
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

    private static async Task CompareJsonObjectsAsync(string expected, string actual)
    {
        var jsonActual = NormalizeNewlines(JsonNode.Parse(actual!)?.ToString());
        var jsonExpected = NormalizeNewlines(JsonNode.Parse(expected!)?.ToString());

        await Assert.That(jsonActual).IsEqualTo(jsonExpected);
    }

    private IDictionary<string, object> GetContent(Type type)
    {
        var isArray = type == typeof(JsonArray) || type == typeof(JArray);

        var jsonContent = isArray ? "[{\"path\":\"folder1\",\"command\":\"add\"}]" : "{\"file1\":{\"script\":[{\"path\":\"folder1\",\"command\":\"add\"}]} }";

        var dict = new Dictionary<string, object>
        {
            {
                "StatusCode", "Created"
            },
            {
                "Content", (isArray ? type == typeof(JArray) ? JArray.Parse(jsonContent) : JsonNode.Parse(jsonContent) : type == typeof(JObject) ? JObject.Parse(jsonContent) : JsonNode.Parse(jsonContent))!
            }
        };
        return dict;
    }

    private string GetExpected(Type type)
    {
        if (type == typeof(JsonArray) || type == typeof(JArray))
        {
            return "[{\"path\":\"folder1\",\"command\":\"add\"}]";
        }
        else
        {
            return "{\"file1\":{\"script\":[{\"path\":\"folder1\",\"command\":\"add\"}]} }";
        }
    }

    private static string? NormalizeNewlines(string? input) => input?.Replace("\r\n", "\n").Replace("\\r\\n", "\\n");

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}

public class TestObject
{
    public string? Data { get; set; }
}
