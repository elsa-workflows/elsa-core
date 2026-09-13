using System.Text.Json;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Core;

public class SerializerUnicodeEncodingTests : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).Build();

    [Test]
    public async Task TestPayloadSerializer()
    {
        var serializer = _serviceProvider.GetRequiredService<IPayloadSerializer>();
        await TestSerializer(input => serializer.Serialize(input));
    }

    [Test]
    public async Task TestApiSerializer()
    {
        var serializer = _serviceProvider.GetRequiredService<IApiSerializer>();
        await TestSerializer(input => serializer.Serialize(input));
    }

    [Test]
    public async Task TestBookmarkPayloadSerializer()
    {
        var serializer = _serviceProvider.GetRequiredService<IBookmarkPayloadSerializer>();
        await TestSerializer(input => serializer.Serialize(input));
    }

    [Test]
    public async Task TestSafeSerializer()
    {
        var serializer = _serviceProvider.GetRequiredService<ISafeSerializer>();
        await TestSerializer(input => serializer.Serialize(input));
    }
    
    [Test]
    public async Task TestWorkflowStateSerializer()
    {
        var serializer = _serviceProvider.GetRequiredService<IWorkflowStateSerializer>();
        await TestSerializer(input => serializer.Serialize(input));
    }

    private static async Task TestSerializer(Func<object, string> serialize)
    {
        var unicodeString = UnicodeRangeGenerator.GenerateUnicodeString();
        var anonymousObject = new
        {
            Text = unicodeString
        };
        var serializedJson = serialize(anonymousObject);
        var serializedStringValue = GetSerializedTextValue(serializedJson);
        await Assert.That(serializedStringValue).IsEqualTo(unicodeString);
    }
    
    private static string GetSerializedTextValue(string serializedJson)
    {
        var rootElement = JsonDocument.Parse(serializedJson).RootElement;
        return GetCaseInsensitiveSerializedTextValue(rootElement, "text", "Text");
    }
    
    private static string GetCaseInsensitiveSerializedTextValue(JsonElement jsonElement, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
            if (jsonElement.TryGetProperty(propertyName, out var property))
                return property.GetString()!;
        throw new KeyNotFoundException($"None of the following properties were found: {string.Join(", ", propertyNames)}");
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_serviceProvider);
}
