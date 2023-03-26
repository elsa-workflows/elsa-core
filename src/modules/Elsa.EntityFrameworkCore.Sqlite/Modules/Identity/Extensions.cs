﻿using Elsa.EntityFrameworkCore.Modules.Identity;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Configures the <see cref="EFCoreIdentityPersistenceFeature"/> feature with SQLite persistence providers.
/// </summary>
public static partial class Extensions
{
    /// <summary>
    /// Configures the <see cref="EFCoreIdentityPersistenceFeature"/> feature with SQLite persistence providers.
    /// </summary>
    /// <param name="feature">The feature to configure.</param>
    /// <param name="connectionString">The connection string to use.</param>
    /// <returns>The configured feature.</returns>
    public static EFCoreIdentityPersistenceFeature UseSqlite(this EFCoreIdentityPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
        return feature;
    }
}