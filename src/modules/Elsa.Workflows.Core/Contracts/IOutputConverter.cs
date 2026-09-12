using System.Text.Json;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Converts a native activity output into a value suitable for one output binding.
/// Implementations must be synchronous, deterministic and side-effect free.
/// </summary>
public interface IOutputConverter
{
    /// <summary>
    /// Converts a non-null native output value into a bound value.
    /// </summary>
    object? Convert(OutputConversionContext context);

    /// <summary>
    /// Validates optional per-binding settings. Returned messages must not include sensitive setting values.
    /// </summary>
    IEnumerable<string> ValidateSettings(JsonElement? settings) => [];
}
