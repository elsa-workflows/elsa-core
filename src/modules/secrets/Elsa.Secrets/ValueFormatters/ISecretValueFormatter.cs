using Elsa.Secrets.Models;

namespace Elsa.Secrets.ValueFormatters
{
    public interface ISecretValueFormatter
    {
        string Type { get; }
        string FormatSecretValue(Secret secret);
    }
}
