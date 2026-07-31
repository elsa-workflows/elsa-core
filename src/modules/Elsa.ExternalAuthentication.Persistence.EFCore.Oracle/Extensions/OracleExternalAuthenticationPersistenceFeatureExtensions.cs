using Elsa.ExternalAuthentication.Persistence.EFCore.Features;
using Elsa.ExternalAuthentication.Persistence.EFCore.Oracle.Configurations;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore;
using Oracle.EntityFrameworkCore.Infrastructure;
using System.Reflection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.Oracle.Extensions;

public static class OracleExternalAuthenticationPersistenceFeatureExtensions
{
    private static Assembly Assembly => typeof(OracleExternalAuthenticationPersistenceFeatureExtensions).Assembly;

    public static EFCoreExternalAuthenticationPersistenceFeature UseOracle(
        this EFCoreExternalAuthenticationPersistenceFeature feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null)
    {
        return feature.UseOracle(_ => connectionString, options, configure);
    }

    public static EFCoreExternalAuthenticationPersistenceFeature UseOracle(
        this EFCoreExternalAuthenticationPersistenceFeature feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null)
    {
        options = options.ConfigureExternalAuthenticationOracle();
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaOracle(Assembly, connectionStringFunc(sp), options, configure);
        return feature;
    }

    /// <summary>
    /// Maps the JSON and protected-payload columns to Oracle LOB types. Their contents routinely exceed the
    /// 2000-byte NVARCHAR2/RAW limit that would otherwise be inferred.
    /// </summary>
    public static ElsaDbContextOptions ConfigureExternalAuthenticationOracle(this ElsaDbContextOptions? options)
    {
        options = options.ConfigureOracle();
        var configurations = new ExternalAuthenticationOracleConfigurations();
        options.ConfigureModel<ExternalAuthenticationElsaDbContext>(modelBuilder =>
        {
            modelBuilder.ApplyConfiguration<PersistedIdentityProviderConnection>(configurations);
            modelBuilder.ApplyConfiguration<PersistedBrokerTransaction>(configurations);
            modelBuilder.ApplyConfiguration<PersistedExternalAuthenticationSession>(configurations);
            modelBuilder.ApplyConfiguration<PersistedConnectionObservation>(configurations);
            modelBuilder.ApplyConfiguration<PersistedPreviewResult>(configurations);
        });
        return options;
    }
}
