using System.Text.Json;
using System.Globalization;
using System.Text.RegularExpressions;
using Elsa.Studio.ExternalAuthentication.Models;

namespace Elsa.Studio.ExternalAuthentication.Services;

/// <summary>Centralizes descriptor-driven value, visibility, validation, and unsafe-setting semantics.</summary>
public static class DescriptorEditorState
{
    public static bool IsVisible(ConnectionFieldDescriptor field, IDictionary<string, JsonElement> settings)
    {
        var condition = field.VisibleWhen;
        return condition is null || settings.TryGetValue(condition.Field, out var value) && string.Equals(ToDisplayString(value), condition.ExpectedValue, StringComparison.Ordinal);
    }

    public static bool IsUnsafeSettingActive(ConnectionFieldDescriptor field, IDictionary<string, JsonElement> settings)
    {
        if (!field.IsUnsafe || !settings.TryGetValue(field.Name, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return false;

        if (value.ValueKind is JsonValueKind.False)
            return false;

        if (string.Equals(field.Name, "providerPkce", StringComparison.Ordinal) && value.ValueKind == JsonValueKind.String && !string.Equals(value.GetString(), "disabled", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public static string ToDisplayString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
        JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
        JsonValueKind.Number or JsonValueKind.Array or JsonValueKind.Object => value.GetRawText(),
        _ => string.Empty
    };

    public static bool GetBoolean(IDictionary<string, JsonElement> settings, string name) =>
        settings.TryGetValue(name, out var value) && value.ValueKind is JsonValueKind.True;

    public static int? GetInteger(IDictionary<string, JsonElement> settings, string name) =>
        settings.TryGetValue(name, out var value) && value.TryGetInt32(out var number) ? number : null;

    public static decimal? GetNumber(IDictionary<string, JsonElement> settings, string name) =>
        settings.TryGetValue(name, out var value) && value.TryGetDecimal(out var number) ? number : null;

    public static void SetString(IDictionary<string, JsonElement> settings, string name, string? value) => settings[name] = JsonSerializer.SerializeToElement(value ?? string.Empty);
    public static void SetBoolean(IDictionary<string, JsonElement> settings, string name, bool value) => settings[name] = JsonSerializer.SerializeToElement(value);
    public static void SetInteger(IDictionary<string, JsonElement> settings, string name, int? value) => SetNullableNumber(settings, name, value);
    public static void SetNumber(IDictionary<string, JsonElement> settings, string name, decimal? value) => SetNullableNumber(settings, name, value);
    public static bool TrySetStructuredValue(IDictionary<string, JsonElement> settings, string name, string value, bool requireStringArray, out string? error)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            if (requireStringArray && (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String)))
            {
                error = "Enter a JSON array containing only strings.";
                return false;
            }
            settings[name] = document.RootElement.Clone();
            error = null;
            return true;
        }
        catch (JsonException)
        {
            error = requireStringArray ? "Enter a valid JSON array containing only strings." : "Enter valid JSON.";
            return false;
        }
    }

    public static bool TrySetAllowedValue(IDictionary<string, JsonElement> settings, ConnectionFieldDescriptor field, string value, out string? error)
    {
        error = null;
        switch (field.ValueType)
        {
            case "boolean" when bool.TryParse(value, out var boolean):
                SetBoolean(settings, field.Name, boolean);
                return true;
            case "integer" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer):
                SetInteger(settings, field.Name, integer);
                return true;
            case "number" when decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number):
                SetNumber(settings, field.Name, number);
                return true;
            case "string-array":
                return TrySetStructuredValue(settings, field.Name, value, true, out error);
            case "json":
                return TrySetStructuredValue(settings, field.Name, value, false, out error);
            default:
                SetString(settings, field.Name, value);
                return true;
        }
    }

    public static IReadOnlyCollection<string> Validate(ConnectionFieldDescriptor field, IDictionary<string, JsonElement> settings)
    {
        if (!IsVisible(field, settings) || field.IsSecretBinding)
            return [];

        settings.TryGetValue(field.Name, out var value);
        var text = ToDisplayString(value);
        var hasValue = value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null && !string.IsNullOrWhiteSpace(text);
        var errors = new List<string>();
        if (field.IsRequired && !hasValue)
            errors.Add($"{field.DisplayName} is required.");
        if (!hasValue)
            return errors;

        var validation = field.Validation;
        if (validation.MinimumLength is { } minimum && text.Length < minimum)
            errors.Add($"{field.DisplayName} must be at least {minimum} characters.");
        if (validation.MaximumLength is { } maximum && text.Length > maximum)
            errors.Add($"{field.DisplayName} must be at most {maximum} characters.");
        if (!string.IsNullOrWhiteSpace(validation.Pattern) && !Regex.IsMatch(text, validation.Pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250)))
            errors.Add($"{field.DisplayName} has an invalid format.");
        if (field.AllowedValues.Count > 0)
        {
            var usesAllowedValues = value.ValueKind == JsonValueKind.Array
                ? value.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String && field.AllowedValues.Contains(item.GetString()!, StringComparer.Ordinal))
                : field.AllowedValues.Contains(text, StringComparer.Ordinal);
            if (!usesAllowedValues)
                errors.Add($"{field.DisplayName} must use only allowed values.");
        }
        if (string.Equals(field.ValueType, "uri", StringComparison.OrdinalIgnoreCase) && (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))))
            errors.Add($"{field.DisplayName} must be an absolute HTTP or HTTPS URI.");
        return errors;
    }

    private static void SetNullableNumber<T>(IDictionary<string, JsonElement> settings, string name, T? value) where T : struct
    {
        if (value is null)
            settings.Remove(name);
        else
            settings[name] = JsonSerializer.SerializeToElement(value.Value);
    }
}
