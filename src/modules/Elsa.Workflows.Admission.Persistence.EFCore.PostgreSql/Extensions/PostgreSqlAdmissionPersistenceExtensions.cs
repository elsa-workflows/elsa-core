using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Workflows.Admission.Persistence.EFCore.Features;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql.Extensions;

public static class PostgreSqlAdmissionPersistenceExtensions
{
    public static EFCoreAdmissionPersistenceFeature UsePostgreSql(this EFCoreAdmissionPersistenceFeature feature,
        string connectionString, ElsaDbContextOptions? options = null, Action<NpgsqlDbContextOptionsBuilder>? configure = null) =>
        feature.UsePostgreSql(_ => connectionString, options, configure);

    public static EFCoreAdmissionPersistenceFeature UsePostgreSql(this EFCoreAdmissionPersistenceFeature feature,
        Func<IServiceProvider, string> connectionString, ElsaDbContextOptions? options = null, Action<NpgsqlDbContextOptionsBuilder>? configure = null)
    {
        feature.Services.AddSingleton<IAdmissionTransactionLock, PostgreSqlAdmissionTransactionLock>();
        feature.Services.AddScoped<IAdmissionDefinitionBootstrapStore, PostgreSqlAdmissionDefinitionBootstrapStore>();
        options ??= new ElsaDbContextOptions();
        options.MigrationsHistoryTableName ??= "__AdmissionMigrationsHistory";
        return feature.UsePostgreSql(typeof(PostgreSqlAdmissionPersistenceExtensions).Assembly, connectionString, options, configure);
    }
}
