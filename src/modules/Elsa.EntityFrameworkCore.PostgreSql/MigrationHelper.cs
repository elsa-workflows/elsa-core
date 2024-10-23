using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.EntityFrameworkCore.PostgreSql;

public static class MigrationHelper
{
    public static void AlterColumnDateTime(MigrationBuilder migrationBuilder, IElsaDbContextSchema schema, string tableName, string columnName, bool nullable)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: $"{columnName}Temp",
            schema: schema.Schema,
            table: tableName,
            type: "timestamptz",
            nullable: true);

        migrationBuilder.Sql(
            $"""
             UPDATE "{schema.Schema}"."{tableName}"
             SET "{columnName}Temp" = to_timestamp("{columnName}", 'YYYY-MM-DD HH24:MI:SS.US') AT TIME ZONE 'UTC'
             WHERE "{columnName}" IS NOT NULL
             """);

        migrationBuilder.DropColumn(
            name: columnName,
            schema: schema.Schema,
            table: tableName);

        migrationBuilder.RenameColumn(
            name: $"{columnName}Temp",
            schema: schema.Schema,
            table: tableName,
            newName: columnName);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: columnName,
            schema: schema.Schema,
            table: tableName,
            type: "timestamptz",
            nullable: nullable,
            oldClrType: typeof(DateTimeOffset),
            oldNullable: nullable);
    }

    public static void AlterColumnBoolean(MigrationBuilder migrationBuilder, IElsaDbContextSchema schema, string tableName, string columnName, bool nullable)
    {
        // Step 1: Add a new temporary nullable column
        migrationBuilder.AddColumn<bool>(
            name: $"{columnName}Temp",
            schema: schema.Schema,
            table: tableName,
            type: "boolean",
            nullable: nullable);

        // Step 2: Update the temporary column with converted values from the original column
        migrationBuilder.Sql(
            $"""
             UPDATE "{schema.Schema}"."{tableName}"
             SET "{columnName}Temp" = CASE WHEN "{columnName}" = 1 THEN TRUE ELSE FALSE END
             WHERE "{columnName}" IS NOT NULL
             """);

        // Step 3: Drop the original column
        migrationBuilder.DropColumn(
            name: columnName,
            schema: schema.Schema,
            table: tableName);

        // Step 4: Rename the temporary column to the original column name
        migrationBuilder.RenameColumn(
            name: $"{columnName}Temp",
            schema: schema.Schema,
            table: tableName,
            newName: columnName);

        // Step 5: Alter the new column to be non-nullable
        migrationBuilder.AlterColumn<bool>(
            name: columnName,
            schema: schema.Schema,
            table: tableName,
            type: "boolean",
            nullable: nullable,
            oldClrType: typeof(bool),
            oldNullable: true);
    }
}