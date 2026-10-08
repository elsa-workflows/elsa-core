using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Options;

internal static class AIProviderOptionsExtensions
{
    public static bool IsProviderEnabled(this IReadOnlyCollection<AIProviderOptions> providerOptions, string providerName)
    {
        if (providerOptions.Count == 0)
            return true;

        return providerOptions.Any(x => x.Enabled &&
                                        (string.Equals(x.Name, providerName, StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(x.Provider, providerName, StringComparison.OrdinalIgnoreCase)));
    }

    public static AIProviderConfiguration ToProviderConfiguration(this AIProviderOptions options) =>
        new()
        {
            Name = options.Name,
            Provider = options.Provider,
            Model = options.Model,
            ApiKeySecretName = options.ApiKeySecretName,
            Endpoint = options.Endpoint
        };
}
