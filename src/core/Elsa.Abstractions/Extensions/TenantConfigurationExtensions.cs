using Elsa.Abstractions.Multitenancy;

namespace Elsa
{
    public static class TenantConfigurationExtensions
    {
        public static string GetDatabaseConnectionString(this TenantConfiguration configuration) => configuration["DatabaseConnectionString"];
        public static string GetPrefix(this TenantConfiguration configuration) => configuration["Prefix"];
    }
}
