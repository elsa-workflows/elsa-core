using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;

namespace Elsa.Persistence.EFCore;

/// <summary>
/// Class That enable Schema change for Migration
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Required to customize migration assembly behavior for schema-aware migrations.")]
public class DbSchemaAwareMigrationAssembly(
    ICurrentDbContext currentContext,
    IDbContextOptions options,
    IMigrationsIdGenerator idGenerator,
    IDiagnosticsLogger<DbLoggerCategory.Migrations> logger)
    : MigrationsAssembly(currentContext, options, idGenerator, logger)
{
    private readonly DbContext _context = currentContext.Context;

    public override Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
    {
        if (activeProvider == null)
            throw new ArgumentNullException(nameof(activeProvider));

        var hasCtorWithSchema = migrationClass.GetConstructor([typeof(IElsaDbContextSchema)]) != null;
        Migration instance;

        if (hasCtorWithSchema && _context is IElsaDbContextSchema schema)
        {
            instance = (Migration)Activator.CreateInstance(migrationClass.AsType(), schema)!;
            instance.ActiveProvider = activeProvider;
        }
        else
            instance = base.CreateMigration(migrationClass, activeProvider);

        if (_context is IElsaDbContextSchema contextSchema && contextSchema.Schema != ElsaDbContextBase.DefaultElsaSchema)
            PatchDesignerSchema(instance, contextSchema.Schema);

        return instance;
    }

    private static void PatchDesignerSchema(Migration migration, string schema)
    {
        foreach (var entityType in migration.TargetModel.GetEntityTypes())
        {
            if (entityType is IMutableEntityType mutableEntityType && entityType.GetSchema() == ElsaDbContextBase.DefaultElsaSchema)
                mutableEntityType.SetSchema(schema);
        }
    }
}