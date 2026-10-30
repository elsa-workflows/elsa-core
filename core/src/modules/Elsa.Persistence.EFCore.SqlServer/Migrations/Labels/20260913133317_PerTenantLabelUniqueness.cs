using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.SqlServer.Migrations.Labels
{
    /// <inheritdoc />
    public partial class PerTenantLabelUniqueness : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public PerTenantLabelUniqueness(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default tenant is "" (not null). Stamp leftover nulls so the filtered unique
            // index (TenantId IS NOT NULL) covers them. Duplicates are not deleted.
            migrationBuilder.Sql($"""
                UPDATE [{_schema.Schema}].[Labels]
                SET [TenantId] = N''
                WHERE [TenantId] IS NULL;
                """);

            // No silent dedupe. List leftover keys and abort; operators must resolve them before upgrading.
            migrationBuilder.Sql($"""
                IF EXISTS (
                    SELECT 1
                    FROM [{_schema.Schema}].[Labels]
                    GROUP BY [TenantId], [NormalizedName]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    DECLARE @DuplicateKeys nvarchar(max);

                    SELECT @DuplicateKeys = STRING_AGG(
                        CAST(CONCAT(N'(', ISNULL([TenantId], N'<null>'), N', ', [NormalizedName], N')') AS nvarchar(max)),
                        N'; '
                    )
                    FROM (
                        SELECT [TenantId], [NormalizedName]
                        FROM [{_schema.Schema}].[Labels]
                        GROUP BY [TenantId], [NormalizedName]
                        HAVING COUNT(*) > 1
                    ) AS Duplicates;

                    DECLARE @ErrorMessage nvarchar(2048);
                    SET @ErrorMessage = CONCAT(
                        N'Cannot create unique index IX_Label_TenantId_NormalizedName because leftover duplicate (TenantId, NormalizedName) rows exist. Operators must resolve leftover (TenantId, NormalizedName) rows before upgrade. Duplicate keys: ',
                        LEFT(@DuplicateKeys, 1500)
                    );
                    THROW 50001, @ErrorMessage, 1;
                END
                """);

            // Name/NormalizedName are not truncated; list over-length Ids and abort before ALTER.
            migrationBuilder.Sql($"""
                IF EXISTS (
                    SELECT 1
                    FROM [{_schema.Schema}].[Labels]
                    WHERE LEN([Name]) > 255 OR LEN([NormalizedName]) > 255
                )
                BEGIN
                    DECLARE @OverLengthIds nvarchar(max);

                    SELECT @OverLengthIds = STRING_AGG(CAST([Id] AS nvarchar(max)), N', ')
                    FROM [{_schema.Schema}].[Labels]
                    WHERE LEN([Name]) > 255 OR LEN([NormalizedName]) > 255;

                    DECLARE @ErrorMessage nvarchar(2048);
                    SET @ErrorMessage = CONCAT(
                        N'Cannot alter Labels.Name / Labels.NormalizedName to nvarchar(255) because leftover rows exceed 255 characters. Operators must shorten or remove those rows before upgrade. Over-length Ids: ',
                        LEFT(@OverLengthIds, 1500)
                    );
                    THROW 50002, @ErrorMessage, 1;
                END
                """);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Labels",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Labels",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                schema: _schema.Schema,
                table: "Labels",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Label_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Labels",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Label_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Labels");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Labels",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                schema: _schema.Schema,
                table: "Labels",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Labels",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);
        }
    }
}
