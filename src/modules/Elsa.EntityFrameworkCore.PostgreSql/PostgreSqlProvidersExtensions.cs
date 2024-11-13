﻿using System.Reflection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use PostgreSQL.
/// </summary>
public static class PostgreSqlProvidersExtensions
{
    private static Assembly Assembly => typeof(PostgreSqlProvidersExtensions).Assembly;

    public static TFeature UsePostgreSql<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<NpgsqlDbContextOptionsBuilder>? configure = null)
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UsePostgreSql(Assembly, connectionString, options, configure);
    }

    public static TFeature UsePostgreSql<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<NpgsqlDbContextOptionsBuilder>? configure = null)
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UsePostgreSql(Assembly, connectionStringFunc, options, configure);
    }

    public static TFeature UsePostgreSql<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature,
        Assembly migrationsAssembly,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<NpgsqlDbContextOptionsBuilder>? configure = null)
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UsePostgreSql(migrationsAssembly, _ => connectionString, options, configure);
    }

    public static TFeature UsePostgreSql<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature,
        Assembly migrationsAssembly,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<NpgsqlDbContextOptionsBuilder>? configure = null)
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaPostgreSql(migrationsAssembly, connectionStringFunc(sp), options, configure: configure);
        return (TFeature)feature;
    }
}