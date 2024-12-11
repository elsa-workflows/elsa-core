namespace Trimble.Elsa.Activities.Activities;

/// <summary>
/// A mapping of a JsonPath to a destination variable.
/// </summary>
public record ResponseMapRecord(string JsonPath, string VariableName)
{
    /// <summary>
    /// Verifies that required properties of the <see cref="ResponseMapRecord"/>
    /// are present and valid.
    /// </summary>
    public void Validate(string JsonPath, string VariableName)
    {
        if (string.IsNullOrWhiteSpace(JsonPath))
        {
            throw new ArgumentException("JsonPath is required.");
        }

        if (string.IsNullOrWhiteSpace(VariableName))
        {
            throw new ArgumentException("VariableName is required.");
        }
    }

    /// <summary>
    /// Strips the root "$." from a JSON path if provided to use in dictionary
    /// look-ups.
    /// </summary>
    public string KeyAsFieldName => JsonPath.Replace("$.", string.Empty);
}
