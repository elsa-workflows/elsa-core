using System.Text.Json;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <inheritdoc />
public class OutputConverterSettingsValidator : IOutputConverterSettingsValidator
{
    /// <inheritdoc />
    public IReadOnlyCollection<string> Validate(
        OutputConverterDescriptor descriptor,
        IOutputConverter converter,
        JsonElement? settings)
    {
        var errors = new List<string>();

        if (settings is { ValueKind: not JsonValueKind.Object })
            errors.Add("Converter settings must be a JSON object.");

        try
        {
            errors.AddRange(converter.ValidateSettings(settings?.Clone()) ?? []);
        }
        catch
        {
            errors.Add("Converter-owned settings validation failed.");
        }

        return errors;
    }
}
