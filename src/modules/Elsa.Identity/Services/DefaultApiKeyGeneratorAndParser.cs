using System.Text;
using Elsa.Identity.Contracts;

namespace Elsa.Identity.Services;

/// <summary>
/// Generates and parses API keys.
/// </summary>
public class DefaultApiKeyGeneratorAndParser : IApiKeyGenerator, IApiKeyParser
{
    /// <inheritdoc />
    public string Generate(string clientId)
    {
        var hexIdentifier = Convert.ToHexString(Encoding.UTF8.GetBytes(clientId));
        var id = Guid.NewGuid().ToString("D");
        return $"{hexIdentifier}-{id}";
    }

    /// <inheritdoc />
    public string Parse(string apiKey)
    {
        var firstSeparatorIndex = apiKey.IndexOf('-');
        var hexIdentifier = apiKey[..firstSeparatorIndex];
        var clientId = Encoding.UTF8.GetString(Convert.FromHexString(hexIdentifier));
        
        return clientId;
    }
}