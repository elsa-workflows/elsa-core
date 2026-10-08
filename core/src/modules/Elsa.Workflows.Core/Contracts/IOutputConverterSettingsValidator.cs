using System.Text.Json;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Validates converter settings against descriptor and converter-owned rules.
/// </summary>
public interface IOutputConverterSettingsValidator
{
    /// <summary>
    /// Validates settings against the descriptor schema and converter-owned validation rules.
    /// </summary>
    IReadOnlyCollection<string> Validate(
        OutputConverterDescriptor descriptor,
        IOutputConverter converter,
        JsonElement? settings);
}
