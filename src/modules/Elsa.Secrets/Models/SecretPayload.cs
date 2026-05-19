namespace Elsa.Secrets.Models;

public class SecretPayload
{
    public string? Value { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public static SecretPayload FromValue(string? value) => new() { Value = value };
}
