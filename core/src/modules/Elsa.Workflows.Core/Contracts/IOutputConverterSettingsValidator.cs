using System.Text.Json;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Validates converter settings using converter-owned rules.
/// </summary>
public interface IOutputConverterSettingsValidator
{
    /// <summary>
    /// Validates settings. <see cref="IOutputConverter.ValidateSettings"/> is the authoritative check.
    /// <see cref="OutputConverterDescriptor.SettingsSchema"/> is descriptor metadata and is not evaluated here.
    /// </summary>
    IReadOnlyCollection<string> Validate(
        OutputConverterDescriptor descriptor,
        IOutputConverter converter,
        JsonElement? settings);
}
