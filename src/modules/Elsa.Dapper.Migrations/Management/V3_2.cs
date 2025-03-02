using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using JetBrains.Annotations;

namespace Elsa.Dapper.Migrations.Management;

/// <inheritdoc />
[Migration(10003, "Elsa:Management:V3.2")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_2 : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
    }

    /// <inheritdoc />
    public override void Down()
    {
    }
}