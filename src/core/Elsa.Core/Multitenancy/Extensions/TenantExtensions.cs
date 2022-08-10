namespace Elsa.Multitenancy.Extensions
{
    public static class TenantExtensions
    {
        public static string? GetDatabaseConnectionString(this ITenant tenant) 
            => tenant.Properties.ContainsKey("DatabaseConnectionString") ? (string)tenant.Properties["DatabaseConnectionString"] : null;

        public static string? GetTenantUrlPrefix(this ITenant tenant)
            => tenant.IsDefault ? null : tenant.Name.ToLowerInvariant().Replace(" ", "-");
    }
}
