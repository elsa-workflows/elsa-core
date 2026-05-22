using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Attributes;
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

    [Fact]
    public void Write_SkipsIndexerProperties()
    {
        var json = JsonSerializer.Serialize<object>(new IndexerModel(), _options);

        Assert.DoesNotContain("\"Item\"", json);
    }

    [Fact]
    public void Write_DoesNotEvaluateExcludedProperty()
    {
        var json = JsonSerializer.Serialize<object>(new ExcludedThrowingModel(), _options);

        Assert.DoesNotContain("\"Secret\"", json);
    }

    [Fact]
    public void Write_DoesNotEvaluateAlwaysIgnoredProperty()
    {
        var json = JsonSerializer.Serialize<object>(new AlwaysIgnoredThrowingModel(), _options);

        Assert.DoesNotContain("\"Secret\"", json);
    }

    [Fact]
    public void Write_IncludesNullableValueTypeWithDefaultUnderlyingValue_WhenIgnoringDefaults()
    {
        var json = JsonSerializer.Serialize<object>(new DefaultIgnoreNullableModel { Count = 0 }, _options);

        Assert.Contains("\"Count\":0", json);
    }

    [Fact]
    public void Write_IncludesProperty_WhenJsonIgnoreConditionIsUnknown()
    {
        var json = JsonSerializer.Serialize<object>(new UnknownIgnoreConditionModel { Name = "Alice" }, _options);

        Assert.Contains("\"Name\":\"Alice\"", json);
    }

    [Fact]
    public void Write_ExcludesStaticProperties()
    {
        var json = JsonSerializer.Serialize<object>(new StaticPropertyModel { Name = "Alice" }, _options);

        Assert.Contains("\"Name\":\"Alice\"", json);
        Assert.DoesNotContain("\"Secret\"", json);
    }

    [Fact]
    public void Write_PreservesDeclarationOrder()
    {
        var json = JsonSerializer.Serialize<object>(new OrderedModel { B = "second", A = "first" }, _options);

        Assert.True(json.IndexOf("\"B\"", StringComparison.Ordinal) < json.IndexOf("\"A\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Write_OrdersInheritedProperties_ByDeclaringTypeThenDeclarationOrder()
    {
        var json = JsonSerializer.Serialize<object>(new DerivedOrderedModel
        {
            BaseB = "base second",
            BaseA = "base first",
            DerivedB = "derived second",
            DerivedA = "derived first"
        }, _options);

        Assert.True(json.IndexOf("\"DerivedB\"", StringComparison.Ordinal) < json.IndexOf("\"DerivedA\"", StringComparison.Ordinal));
        Assert.True(json.IndexOf("\"DerivedA\"", StringComparison.Ordinal) < json.IndexOf("\"BaseB\"", StringComparison.Ordinal));
        Assert.True(json.IndexOf("\"BaseB\"", StringComparison.Ordinal) < json.IndexOf("\"BaseA\"", StringComparison.Ordinal));
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

    private sealed class IndexerModel
    {
        public string this[int index] => index.ToString();
    }

    private sealed class ExcludedThrowingModel
    {
        [ExcludeFromHash]
        public string Secret => throw new InvalidOperationException();
    }

    private sealed class AlwaysIgnoredThrowingModel
    {
        [JsonIgnore]
        public string Secret => throw new InvalidOperationException();
    }

    private sealed class DefaultIgnoreNullableModel
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int? Count { get; set; }
    }

    private sealed class UnknownIgnoreConditionModel
    {
        [JsonIgnore(Condition = (JsonIgnoreCondition)999)]
        public string? Name { get; set; }
    }

    private sealed class StaticPropertyModel
    {
        public static string Secret => throw new InvalidOperationException();

        public string? Name { get; set; }
    }

    private sealed class OrderedModel
    {
        public string? B { get; set; }

        public string? A { get; set; }
    }

    private class BaseOrderedModel
    {
        public string? BaseB { get; set; }

        public string? BaseA { get; set; }
    }

    private sealed class DerivedOrderedModel : BaseOrderedModel
    {
        public string? DerivedB { get; set; }

        public string? DerivedA { get; set; }
    }
}
