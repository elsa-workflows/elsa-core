using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Expressions.Services;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.ReferenceHandlers;

namespace Elsa.Workflows.IntegrationTests.Serialization.Polymorphism;

public class Tests
{
    [Fact(DisplayName = "Objects mixed with primitive, complex, expando objects and arrays of these can be serialized")]
    public void Test1()
    {
        var model = CreateModel();
        var expectedJson = File.ReadAllText("Serialization/Polymorphism/data.json");
        var actualJson = JsonSerializer.Serialize(model, GetSerializerOptions());
        Assert.Equal(expectedJson, actualJson);
    }

    [Fact(DisplayName = "Objects mixed with primitive, complex, expando objects and arrays of these can be round-tripped")]
    public void Test2()
    {
        var model = CreateModel();
        var json = JsonSerializer.Serialize(model, GetSerializerOptions());
        var deserializedModel = JsonSerializer.Deserialize<Model>(json, GetSerializerOptions());
        var roundTrippedJson = JsonSerializer.Serialize(deserializedModel, GetSerializerOptions());
        Assert.Equal(json, roundTrippedJson);
    }

    private Model CreateModel()
    {
        // Create a model with various nested properties
        var metadata = new ExpandoObject() as IDictionary<string, object>;

        // Initialize metadata with test data.
        metadata["Foo"] = "Bar";
        metadata["Number"] = 123;
        metadata["Flag"] = true;
        metadata["Models"] = new List<Model>
        {
            new(Text: "Hello", Number: 1, Flag: true, Metadata: metadata),
            new(Text: "World", Number: 2)
        };
        metadata["CustomDictionary"] = new CustomDictionary { ["content-type"] = new[] { "application/json" } };

        return new Model(
            "Hello World",
            123,
            true,
            new List<Model>
            {
                new("Hello", 1, true, new List<Model>() { new(Metadata: metadata) }),
                new("World", 2)
            },
            metadata,
            new Model("Payload"),
            new HashSet<Model>
            {
                new()
                {
                    Text = "I'm a model in a set!"
                }
            },
            new Dictionary<string, Model>
            {
                ["Hello"] = new("Hello", 1, true),
                ["World"] = new("World", 2),
            }
        );
    }

    private JsonSerializerOptions GetSerializerOptions()
    {
        var referenceHandler = new CrossScopedReferenceHandler();
        var options = new JsonSerializerOptions
        {
            ReferenceHandler = referenceHandler,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(JsonMetadataServices.TimeSpanConverter);
        options.Converters.Add(new PolymorphicObjectConverterFactory(new WellKnownTypeRegistry()));
        return options;
    }
}