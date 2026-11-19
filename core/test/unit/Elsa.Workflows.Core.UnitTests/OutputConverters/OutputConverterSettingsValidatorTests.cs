using System.Text.Json;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class OutputConverterSettingsValidatorTests
{
    private readonly OutputConverterSettingsValidator _validator = new();

    [Fact]
    public void Validate_WhenSettingsAreNotAnObject_ReturnsObjectError()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var converter = new RecordingConverter();
        var settings = ParseJson("[]");

        // Act
        var errors = _validator.Validate(descriptor, converter, settings);

        // Assert
        Assert.Contains("Converter settings must be a JSON object.", errors);
    }

    [Fact]
    public void Validate_WhenSettingsSchemaWouldRejectMissingRequiredProperty_DoesNotEvaluateSchema()
    {
        // Arrange
        var descriptor = CreateDescriptor(
            """
            {
              "type": "object",
              "required": ["prefix"],
              "properties": {
                "prefix": { "type": "string" }
              }
            }
            """);
        var converter = new RecordingConverter();
        var settings = ParseJson("{}");

        // Act
        var errors = _validator.Validate(descriptor, converter, settings);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenSettingsSchemaWouldRejectWrongType_DoesNotEvaluateSchema()
    {
        // Arrange
        var descriptor = CreateDescriptor(
            """
            {
              "type": "object",
              "properties": {
                "count": { "type": "integer", "minimum": 1, "maximum": 10 }
              }
            }
            """);
        var converter = new RecordingConverter();
        var settings = ParseJson("""{ "count": "not-a-number" }""");

        // Act
        var errors = _validator.Validate(descriptor, converter, settings);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenSettingsSchemaWouldRejectEnumOrPattern_DoesNotEvaluateSchema()
    {
        // Arrange
        var descriptor = CreateDescriptor(
            """
            {
              "type": "object",
              "properties": {
                "mode": { "type": "string", "enum": ["compact", "verbose"] },
                "code": { "type": "string", "pattern": "^[A-Z]{3}$" }
              }
            }
            """);
        var converter = new RecordingConverter();
        var settings = ParseJson("""{ "mode": "unsupported", "code": "abc" }""");

        // Act
        var errors = _validator.Validate(descriptor, converter, settings);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenValidateSettingsReturnsErrors_ReturnsThoseErrors()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var converter = new RecordingConverter(_ => ["The configured format is unsupported."]);
        var settings = ParseJson("""{ "format": "unsupported" }""");

        // Act
        var errors = _validator.Validate(descriptor, converter, settings);

        // Assert
        Assert.Equal(["The configured format is unsupported."], errors);
    }

    [Fact]
    public void Validate_WhenValidateSettingsThrows_ReturnsGenericFailure()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var converter = new ThrowingConverter();
        var settings = ParseJson("{}");

        // Act
        var errors = _validator.Validate(descriptor, converter, settings);

        // Assert
        Assert.Equal(["Converter-owned settings validation failed."], errors);
    }

    [Fact]
    public void Validate_WhenSettingsAreNullAndValidateSettingsAcceptsThem_ReturnsNoErrors()
    {
        // Arrange
        var descriptor = CreateDescriptor(
            """
            {
              "type": "object",
              "required": ["prefix"]
            }
            """);
        var converter = new RecordingConverter();

        // Act
        var errors = _validator.Validate(descriptor, converter, settings: null);

        // Assert
        Assert.Empty(errors);
    }

    private static OutputConverterDescriptor CreateDescriptor(string? settingsSchema = null) =>
        new(
            "tests.settings-validator",
            typeof(int),
            typeof(string),
            "Test",
            settingsSchema: settingsSchema is null ? null : ParseJson(settingsSchema));

    private static JsonElement ParseJson(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private sealed class RecordingConverter(Func<JsonElement?, IEnumerable<string>>? validate = null) : IOutputConverter
    {
        public object? Convert(OutputConversionContext context) => context.Value;

        public IEnumerable<string> ValidateSettings(JsonElement? settings) => validate?.Invoke(settings) ?? [];
    }

    private sealed class ThrowingConverter : IOutputConverter
    {
        public object? Convert(OutputConversionContext context) => context.Value;

        public IEnumerable<string> ValidateSettings(JsonElement? settings) => throw new InvalidOperationException("boom");
    }
}
