using System.Collections.Concurrent;
using System.Text.Json;
using Elsa.Workflows.Models;
using Json.Schema;

namespace Elsa.Workflows;

/// <inheritdoc />
public class OutputConverterSettingsValidator : IOutputConverterSettingsValidator
{
    private static readonly JsonElement EmptySettings = JsonDocument.Parse("{}").RootElement.Clone();
    private readonly ConcurrentDictionary<string, SchemaValidationPlan> _schemaValidationPlans = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyCollection<string> Validate(
        OutputConverterDescriptor descriptor,
        IOutputConverter converter,
        JsonElement? settings)
    {
        var errors = new List<string>();

        if (settings is { ValueKind: not JsonValueKind.Object })
            errors.Add("Converter settings must be a JSON object.");

        if (descriptor.SettingsSchema is { } schemaElement)
        {
            var plan = _schemaValidationPlans.GetOrAdd(descriptor.Id, _ => CreateSchemaValidationPlan(schemaElement));

            if (plan.Schema == null)
                errors.Add("The registered converter settings schema is invalid.");
            else if (!plan.Schema.Evaluate(settings ?? EmptySettings).IsValid)
                errors.Add("Converter settings do not satisfy the registered JSON Schema.");
        }

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

    private static SchemaValidationPlan CreateSchemaValidationPlan(JsonElement schemaElement)
    {
        try
        {
            return new(JsonSchema.Build(schemaElement));
        }
        catch
        {
            return new(null);
        }
    }

    private sealed record SchemaValidationPlan(JsonSchema? Schema);
}
