using Elsa.Common.Multitenancy;
using Elsa.EntityFrameworkCore.Extensions;

namespace Elsa.Server.Web.Extensions;

public static class MultitenantConnectionStringServiceProviderExtensions
{
    public static string GetSqliteConnectionString(this IServiceProvider serviceProvider) => serviceProvider.GetConnectionString("Sqlite")!;
    public static string GetSqlServerConnectionString(this IServiceProvider serviceProvider) => serviceProvider.GetConnectionString("SqlServer")!;
    public static string GetMySqlConnectionString(this IServiceProvider serviceProvider) => serviceProvider.GetConnectionString("MySql")!;
    public static string GetPostgreSqlConnectionString(this IServiceProvider serviceProvider) => serviceProvider.GetConnectionString("PostgreSql")!;

    public static string? GetConnectionString(this IServiceProvider serviceProvider, string name)
    {
        var tenantAccessor = serviceProvider.GetRequiredService<ITenantAccessor>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var tenant = tenantAccessor.CurrentTenant;
        var defaultConnectionString = configuration.GetConnectionString(name)!;
        var connectionString = tenant?.GetConnectionString(name) ?? defaultConnectionString;
        return connectionString;
    }
}