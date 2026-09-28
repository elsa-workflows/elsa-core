using System.Diagnostics.CodeAnalysis;
using FluentMigrator;
using JetBrains.Annotations;
using static System.Int32;

namespace Elsa.Persistence.Dapper.Migrations.Runtime;

/// <summary>
/// Creates the <c>KeyValues</c> table used by <c>DapperKeyValueStore</c> (#263)
/// and adds <c>BookmarkQueueItems.SerializedOptions</c> (#264).
/// Both operations are existence-guarded so hand-created workarounds keep working.
/// </summary>
[Migration(20008, "Elsa:Runtime:V3.9")]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class V3_9 : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        if (!Schema.Table("KeyValues").Exists())
            Create.Table("KeyValues")
                .WithColumn("Id").AsString().PrimaryKey()
                .WithColumn("TenantId").AsString().Nullable()
                .WithColumn("Value").AsString(MaxValue).Nullable();

        if (!Schema.Table("BookmarkQueueItems").Column("SerializedOptions").Exists())
            Alter.Table("BookmarkQueueItems").AddColumn("SerializedOptions").AsString(MaxValue).Nullable();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Leave KeyValues and SerializedOptions in place. Rolling them back would
    /// drop a hand-created KeyValues table or SerializedOptions column and their data.
    /// </remarks>
    public override void Down()
    {
    }
}
