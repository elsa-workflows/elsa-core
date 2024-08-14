using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using JetBrains.Annotations;

namespace Elsa.Dapper.Migrations.Identity;

/// <inheritdoc />
[Migration(30002, "Elsa:Identity:V3.1")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_1 : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        // No changes
    }

    /// <inheritdoc />
    public override void Down()
    {
        // No changes
    }
}