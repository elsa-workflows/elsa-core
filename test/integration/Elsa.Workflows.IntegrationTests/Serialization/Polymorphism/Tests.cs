using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.ReferenceHandlers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Elsa.Workflows.IntegrationTests.Serialization.Polymorphism;

public class Tests
{
    [Fact(DisplayName = "Objects mixed with primitive, complex, expando objects and arrays of these can be serialized")]
    public void Test1()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var model = CreateModel();
        var expectedJson = File.ReadAllText("Serialization/Polymorphism/data.json");
        var actualJson = JsonSerializer.Serialize(model, GetSerializerOptions(serviceProvider));
        Assert.Equal(expectedJson, actualJson);
    }

    [Fact(DisplayName = "Objects mixed with primitive, complex, expando objects and arrays of these can be round-tripped")]
    public void Test2()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var model = CreateModel();
        var json = JsonSerializer.Serialize(model, GetSerializerOptions(serviceProvider));
        var deserializedModel = JsonSerializer.Deserialize<Model>(json, GetSerializerOptions(serviceProvider));
        var roundTrippedJson = JsonSerializer.Serialize(deserializedModel, GetSerializerOptions(serviceProvider));
        Assert.Equal(json, roundTrippedJson);
    }

    [Fact(DisplayName = "Object with unknown typ will be deserialized as ExpandoObject")]
    public void Test3()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var expectedJson = File.ReadAllText("Serialization/Polymorphism/dataWithUnknownType.json");
        var actualType = JsonSerializer.Deserialize<object>(expectedJson, GetSerializerOptions(serviceProvider));
        Assert.IsAssignableFrom<ExpandoObject>(actualType);
    }

    [Fact(DisplayName = "Object with unknown typ will be deserialized as real type")]
    public void Test4()
    {
        var dynamicPolymorphicObjectTypeResolver = Substitute.For<IDynamicPolymorphicObjectTypeResolver>();
        dynamicPolymorphicObjectTypeResolver.GetType("XYZ.Plugin.Payload, PluginActivities")
                               .Throws(new ResolveTypeException());

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDynamicPolymorphicObjectTypeResolver))
                               .Returns(dynamicPolymorphicObjectTypeResolver);

        var expectedJson = File.ReadAllText("Serialization/Polymorphism/dataWithUnknownType.json");
        var exception = Assert.Throws<ResolveTypeException>(() => JsonSerializer.Deserialize<object>(expectedJson, GetSerializerOptions(serviceProvider)));
        Assert.NotNull(exception);
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

    private JsonSerializerOptions GetSerializerOptions(IServiceProvider serviceProvider)
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
        options.Converters.Add(new PolymorphicObjectConverterFactory(serviceProvider));
        return options;
    }

    private class ResolveTypeException : Exception
    {

    }
}