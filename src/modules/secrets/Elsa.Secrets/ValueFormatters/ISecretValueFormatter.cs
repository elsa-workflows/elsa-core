using System.Threading.Tasks;
using Elsa.Secrets.Models;

namespace Elsa.Secrets.ValueFormatters
{
    public interface ISecretValueFormatter
    {
        string Type { get; }
        Task<string> FormatSecretValue(Secret secret);
    }
}
