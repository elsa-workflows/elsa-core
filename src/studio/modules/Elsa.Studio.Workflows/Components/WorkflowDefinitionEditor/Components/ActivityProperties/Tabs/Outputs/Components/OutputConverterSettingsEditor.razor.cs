using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Outputs.Components;

/// <summary>
/// Edits JSON settings for a selected output converter.
/// </summary>
public partial class OutputConverterSettingsEditor
{
    private readonly List<SettingsField> _fields = [];
    private JsonObject _settings = [];
    private string _rawSettings = "{}";
    private string? _error;
    private string? _settingsSignature;

    /// <summary>
    /// The optional JSON schema provided by the selected converter descriptor.
    /// </summary>
    [Parameter] public JsonElement? SettingsSchema { get; set; }

    /// <summary>
    /// The current converter settings.
    /// </summary>
    [Parameter] public JsonObject? Settings { get; set; }

    /// <summary>
    /// Raised when valid converter settings change.
    /// </summary>
    [Parameter] public EventCallback<JsonObject> SettingsChanged { get; set; }

    /// <summary>
    /// Gets or sets whether settings are read-only.
    /// </summary>
    [Parameter] public bool IsReadOnly { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var schemaSignature = SettingsSchema?.GetRawText() ?? string.Empty;
        var settingsSignature = $"{schemaSignature}\n{Settings?.ToJsonString() ?? "{}"}";
        if (settingsSignature == _settingsSignature)
            return;

        _settingsSignature = settingsSignature;
        _settings = Settings?.DeepClone().AsObject() ?? [];
        _rawSettings = _settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        _error = null;
        BuildFields();
    }

    private void BuildFields()
    {
        _fields.Clear();
        if (SettingsSchema is not { ValueKind: JsonValueKind.Object } schema ||
            !schema.TryGetProperty("type", out var type) || type.GetString() != "object" ||
            !schema.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object)
            return;

        var required = schema.TryGetProperty("required", out var requiredElement) && requiredElement.ValueKind == JsonValueKind.Array
            ? requiredElement.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()).ToHashSet()
            : [];

        foreach (var property in properties.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                _fields.Clear();
                return;
            }

            var propertySchema = property.Value;
            var fieldType = propertySchema.TryGetProperty("type", out var fieldTypeElement) ? fieldTypeElement.GetString() : null;
            var options = propertySchema.TryGetProperty("enum", out var enumElement) && enumElement.ValueKind == JsonValueKind.Array
                ? enumElement.EnumerateArray().Where(x => x.ValueKind is JsonValueKind.String or JsonValueKind.Number).Select(x => x.ToString()).ToList()
                : [];

            if (fieldType == null && propertySchema.TryGetProperty("enum", out enumElement) && enumElement.ValueKind == JsonValueKind.Array)
                fieldType = enumElement.EnumerateArray().All(x => x.ValueKind == JsonValueKind.Number) ? "number" : "string";

            if (fieldType is not ("string" or "number" or "integer" or "boolean") && options.Count == 0)
            {
                _fields.Clear();
                return;
            }

            if (options.Count > 0 && fieldType is not (null or "string" or "number" or "integer"))
            {
                _fields.Clear();
                return;
            }

            var label = propertySchema.TryGetProperty("title", out var title) ? title.GetString() : null;
            var description = propertySchema.TryGetProperty("description", out var descriptionElement) ? descriptionElement.GetString() : null;
            var field = new SettingsField(property.Name, label ?? property.Name, description ?? string.Empty, fieldType ?? "string", options, required.Contains(property.Name));
            _fields.Add(field);
        }
    }

    private string? GetTextValue(string name) => _settings[name]?.ToString();

    private bool GetBooleanValue(string name) =>
        _settings[name] is JsonValue value && value.TryGetValue<bool>(out var boolean) && boolean;

    private async Task UpdateEnumAsync(SettingsField field, string value)
    {
        await UpdateTextAsync(field, value);
    }

    private async Task UpdateTextAsync(SettingsField field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (field.IsRequired)
            {
                _error = Localizer["This setting is required."];
                return;
            }

            _settings.Remove(field.Name);
        }
        else if (field.Type == "number")
        {
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
            {
                _error = Localizer["Enter a valid number."];
                return;
            }

            _settings[field.Name] = number;
        }
        else if (field.Type == "integer")
        {
            if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
            {
                _error = Localizer["Enter a valid integer."];
                return;
            }

            _settings[field.Name] = integer;
        }
        else
        {
            _settings[field.Name] = value;
        }

        _error = null;
        await RaiseSettingsChangedAsync();
    }

    private async Task UpdateBooleanAsync(string name, bool value)
    {
        _settings[name] = value;
        _error = null;
        await RaiseSettingsChangedAsync();
    }

    private async Task OnRawSettingsChangedAsync(string value)
    {
        _rawSettings = value;

        try
        {
            var settings = JsonNode.Parse(value) as JsonObject;
            if (settings == null)
                throw new JsonException();

            _settings = settings;
            _error = null;
            await RaiseSettingsChangedAsync();
        }
        catch (JsonException)
        {
            _error = Localizer["Converter settings must be a valid JSON object."];
        }
    }

    private Task RaiseSettingsChangedAsync()
    {
        _settingsSignature = _settings.ToJsonString();
        return SettingsChanged.InvokeAsync(_settings.DeepClone().AsObject());
    }

    private sealed record SettingsField(string Name, string Label, string Description, string Type, ICollection<string> Options, bool IsRequired);
}
