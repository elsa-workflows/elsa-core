using FluentMigrator;
using JetBrains.Annotations;

namespace Elsa.Dapper.Migrations.Identity;

/// <inheritdoc />
[Migration(30001, "Elsa:Identity:Initial")]
[PublicAPI]
public class Initial : Migration
{
    /// <inheritdoc />
    public override void Up()
    {
        Create
            .Table("Users")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("Name").AsString().NotNullable()
            .WithColumn("HashedPassword").AsString().NotNullable()
            .WithColumn("HashedPasswordSalt").AsString().NotNullable()
            .WithColumn("Roles").AsString().NotNullable();
        
        Create
            .Table("Roles")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("Name").AsString().NotNullable()
            .WithColumn("Permissions").AsString().NotNullable();

        Create
            .Table("Applications")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("ClientId").AsString().NotNullable()
            .WithColumn("HashedClientSecret").AsString().NotNullable()
            .WithColumn("HashedClientSecretSalt").AsString().NotNullable()
            .WithColumn("Name").AsString().NotNullable()
            .WithColumn("HashedApiKey").AsString().NotNullable()
            .WithColumn("HashedApiKeySalt").AsString().NotNullable()
            .WithColumn("Roles").AsString().NotNullable();
    }

    /// <inheritdoc />
    public override void Down()
    {
        Delete.Table("Users");
        Delete.Table("Roles");
        Delete.Table("Applications");
    }
}