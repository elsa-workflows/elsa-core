using System.Reflection;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Features;
using Elsa.Secrets.Persistence.EFCore.Oracle.Configurations;
using Elsa.Secrets.Models;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Elsa.Secrets.Persistence.EFCore.Oracle.Extensions;

public static class OracleSecretsPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(OracleSecretsPersistenceFeatureExtensions).Assembly;

    public static EFCoreSecretsPersistenceFeature UseOracle(
        this EFCoreSecretsPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseOracle(_ => connectionString, options, configure);
    }

    public static EFCoreSecretsPersistenceFeature UseOracle(
        this EFCoreSecretsPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null)
    {
        options = options.ConfigureSecretsOracle();
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaOracle(Assembly, connectionStringFunc(sp), options, configure);
        return feature;
    }

    public static ElsaDbContextOptions ConfigureSecretsOracle(this ElsaDbContextOptions? options)
    {
        options = options.ConfigureOracle();
        var secrets = new SecretsConfiguration();
        options.ConfigureModel<SecretsElsaDbContext>(modelBuilder => modelBuilder.ApplyConfiguration<Secret>(secrets));
        return options;
    }
}
