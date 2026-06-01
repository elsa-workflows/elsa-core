using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace Elsa.Persistence.EntityFramework.Core;

/// <summary>
/// Treats schema-only model drift as equivalent so Elsa consumers can use custom schemas without
/// tripping EF Core pending model checks.
/// </summary>
public class DbSchemaAwareMigrationsModelDiffer : MigrationsModelDiffer
{
    private ElsaDbContextOptions _dbOptions;
#if NET8_0
    public DbSchemaAwareMigrationsModelDiffer(
        IRelationalTypeMappingSource typeMappingSource,
        IMigrationsAnnotationProvider migrationsAnnotationProvider,
        IRowIdentityMapFactory rowIdentityMapFactory,
        CommandBatchPreparerDependencies commandBatchPreparerDependencies,
        ElsaDbContextOptions dbOptions)
        : base(typeMappingSource, migrationsAnnotationProvider, rowIdentityMapFactory, commandBatchPreparerDependencies)
    {
        _dbOptions = dbOptions;
    }
#elif NET9_0 || NET10_0
    public DbSchemaAwareMigrationsModelDiffer(
        IRelationalTypeMappingSource typeMappingSource,
        IMigrationsAnnotationProvider migrationsAnnotationProvider,
        IRelationalAnnotationProvider relationalAnnotationProvider,
        IRowIdentityMapFactory rowIdentityMapFactory,
        CommandBatchPreparerDependencies commandBatchPreparerDependencies,
        ElsaDbContextOptions dbOptions)
        : base(typeMappingSource, migrationsAnnotationProvider, relationalAnnotationProvider, rowIdentityMapFactory, commandBatchPreparerDependencies)
    {
        _dbOptions = dbOptions;
    }
#elif NETSTANDARD2_1
    public DbSchemaAwareMigrationsModelDiffer(
        IRelationalTypeMappingSource typeMappingSource,
        IMigrationsAnnotationProvider migrationsAnnotations,
        Microsoft.EntityFrameworkCore.ChangeTracking.Internal.IChangeDetector changeDetector,
        Microsoft.EntityFrameworkCore.Update.IUpdateAdapterFactory updateAdapterFactory,
        CommandBatchPreparerDependencies commandBatchPreparerDependencies,
        ElsaDbContextOptions dbOptions)
        : base(typeMappingSource, migrationsAnnotations, changeDetector, updateAdapterFactory, commandBatchPreparerDependencies)
    {
        _dbOptions = dbOptions;
    }
#else
#error Unsupported target framework for SchemaAwareMigrationsModelDiffer.
#endif

    public override bool HasDifferences(IRelationalModel? source, IRelationalModel? target)
        => source is null || target is null
            ? base.HasDifferences(source, target)
            : HasSchemaOnlyDifference(source, target) ? false : base.HasDifferences(source, target);

    public override IReadOnlyList<MigrationOperation> GetDifferences(IRelationalModel? source, IRelationalModel? target)
        => source is null || target is null
            ? base.GetDifferences(source, target)
            : HasSchemaOnlyDifference(source, target) ? Array.Empty<MigrationOperation>() : base.GetDifferences(source, target);

    private bool HasSchemaOnlyDifference(IRelationalModel source, IRelationalModel target)
    {
        var sourceSchema = source.Model.GetDefaultSchema();
        var targetSchema = target.Model.GetDefaultSchema();

        if (string.Equals(sourceSchema, targetSchema, StringComparison.Ordinal))
            return false;

        if (string.IsNullOrEmpty(_dbOptions.SchemaName))
        {
            return false;
        }
        
        var sourceDebug = ReplaceSchemas(source.ToDebugString(MetadataDebugStringOptions.SingleLineDefault, 0), sourceSchema, _dbOptions.SchemaName);
        var targetDebug = target.ToDebugString(MetadataDebugStringOptions.SingleLineDefault, 0);

        return string.Equals(sourceDebug, targetDebug, StringComparison.Ordinal);
    }

    private static string ReplaceSchemas(string debugString, string toReplace, string runtimeSchema)
    {
        return Regex.Replace(debugString, Regex.Escape(toReplace!), runtimeSchema, RegexOptions.CultureInvariant);
    }
}