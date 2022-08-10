using Microsoft.Extensions.Configuration;

namespace Elsa.Extensions
{
    public static class ConfigurationExtensions
    {
        public static bool GetIsMultitenancyEnabled(this IConfiguration configuration)
            => configuration.GetValue<bool>("Elsa:Multitenancy:Enabled");
    }
}
