using Elsa.Abstractions.Multitenancy;

namespace Elsa
{
    public static class TenantExtensions
    {
        public static string GetDatabaseConnectionString(this ITenant tenant) => tenant.Configuration["DatabaseConnectionString"];
        public static string GetPrefix(this ITenant tenant) => tenant.Configuration["Prefix"];
    }
}
