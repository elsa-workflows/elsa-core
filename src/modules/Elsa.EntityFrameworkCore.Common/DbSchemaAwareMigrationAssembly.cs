using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;

namespace Elsa.EntityFrameworkCore;

/// <summary>
/// Class That enable Schema change for Migration
/// </summary>
public class DbSchemaAwareMigrationAssembly : MigrationsAssembly
{
    private readonly DbContext _context;

    public DbSchemaAwareMigrationAssembly(ICurrentDbContext currentContext,
        IDbContextOptions options, IMigrationsIdGenerator idGenerator,
        IDiagnosticsLogger<DbLoggerCategory.Migrations> logger)
        : base(currentContext, options, idGenerator, logger)
    {
        _context = currentContext.Context;
    }

    public override Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
    {
        if (activeProvider == null)
            throw new ArgumentNullException(nameof(activeProvider));

        var hasCtorWithSchema = migrationClass.GetConstructor([typeof(IElsaDbContextSchema)]) != null;

        if (hasCtorWithSchema && _context is IElsaDbContextSchema schema)
        {
            var instance = (Migration)Activator.CreateInstance(migrationClass.AsType(), schema)!;
            instance.ActiveProvider = activeProvider;
            return instance;
        }

        return base.CreateMigration(migrationClass, activeProvider);
    }
}