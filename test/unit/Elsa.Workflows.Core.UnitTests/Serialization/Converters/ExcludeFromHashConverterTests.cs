using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Workflows.Core.UnitTests.Serialization.Converters;

public class ExcludeFromHashConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new ExcludeFromHashConverterFactory() }
    };

    [Fact]
    public void Write_IncludesConditionallyIgnoredProperty_WhenValueIsNotNull()
    {
        var json = JsonSerializer.Serialize<object>(new ConditionalIgnoreModel { Name = "Alice" }, _options);

        Assert.Contains("\"Name\":\"Alice\"", json);
    }

    [Fact]
    public void Write_ExcludesConditionallyIgnoredProperty_WhenValueIsNull()
    {
        var json = JsonSerializer.Serialize<object>(new ConditionalIgnoreModel(), _options);

        Assert.DoesNotContain("\"Name\"", json);
    }

    [Fact]
    public void Write_ExcludesAlwaysIgnoredProperty()
    {
        var json = JsonSerializer.Serialize<object>(new AlwaysIgnoreModel { Secret = "hidden" }, _options);

        Assert.DoesNotContain("\"Secret\"", json);
    }

    private sealed class ConditionalIgnoreModel
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }
    }

    private sealed class AlwaysIgnoreModel
    {
        [JsonIgnore]
        public string? Secret { get; set; }
    }
}
