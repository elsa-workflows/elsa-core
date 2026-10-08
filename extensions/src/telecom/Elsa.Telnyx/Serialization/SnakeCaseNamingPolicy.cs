using System.Text.Json;
using Humanizer;

namespace Elsa.Telnyx.Serialization;

/// <summary>
/// Reads and writes names using snake_case casing. 
/// </summary>
public sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    /// <inheritdoc />
    public override string ConvertName(string name) => name.Underscore();
}