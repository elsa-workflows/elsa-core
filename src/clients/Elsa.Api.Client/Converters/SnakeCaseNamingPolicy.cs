using System.Text.Json;
using Humanizer;

namespace Elsa.Api.Client.Converters;

/// <summary>
/// Reads and writes names using snake_case casing. 
/// </summary>
public sealed class PascalCaseNamingPolicy : JsonNamingPolicy
{
    /// <inheritdoc />
    public override string ConvertName(string name) => name.Pascalize();
}