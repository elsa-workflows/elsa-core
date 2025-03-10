using System.Text.Json;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Core;

public class SerializerUnicodeEncodingTests(ITestOutputHelper testOutputHelper)
{
    private readonly IServiceProvider _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();

    [Fact]
    public void TestPayloadSerializer()
    {
        var serializer = _serviceProvider.GetRequiredService<IPayloadSerializer>();
        TestSerializer(input => serializer.Serialize(input));
    }

    [Fact]
    public void TestApiSerializer()
    {
        var serializer = _serviceProvider.GetRequiredService<IApiSerializer>();
        TestSerializer(input => serializer.Serialize(input));
    }

    [Fact]
    public void TestBookmarkPayloadSerializer()
    {
        var serializer = _serviceProvider.GetRequiredService<IBookmarkPayloadSerializer>();
        TestSerializer(input => serializer.Serialize(input));
    }

    [Fact]
    public void TestSafeSerializer()
    {
        var serializer = _serviceProvider.GetRequiredService<ISafeSerializer>();
        TestSerializer(input => serializer.Serialize(input));
    }
    
    [Fact]
    public void TestWorkflowStateSerializer()
    {
        var serializer = _serviceProvider.GetRequiredService<IWorkflowStateSerializer>();
        TestSerializer(input => serializer.Serialize(input));
    }

    private void TestSerializer(Func<object, string> serialize)
    {
        var unicodeString = UnicodeRangeGenerator.GenerateUnicodeString();
        var anonymousObject = new
        {
            Text = unicodeString
        };
        var serializedJson = serialize(anonymousObject);
        var serializedStringValue = GetSerializedTextValue(serializedJson);
        Assert.Equal(unicodeString, serializedStringValue);
    }
    
    private string GetSerializedTextValue(string serializedJson)
    {
        var rootElement = JsonDocument.Parse(serializedJson).RootElement;
        return GetCaseInsensitiveSerializedTextValue(rootElement, "text", "Text");
    }
    
    private string GetCaseInsensitiveSerializedTextValue(JsonElement jsonElement, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
            if (jsonElement.TryGetProperty(propertyName, out var property))
                return property.GetString()!;
        throw new KeyNotFoundException($"None of the following properties were found: {string.Join(", ", propertyNames)}");
    }
}