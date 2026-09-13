using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Oracle.Migrations.Labels
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
                UPDATE "{_schema.Schema}"."Labels"
                SET "TenantId" = ''
                WHERE "TenantId" IS NULL;
                """);

            // No silent dedupe. List leftover keys and abort; operators must resolve them before upgrading.
            migrationBuilder.Sql($"""
                DECLARE
                    duplicate_keys VARCHAR2(4000);
                    duplicate_count NUMBER;
                BEGIN
                    SELECT COUNT(*)
                    INTO duplicate_count
                    FROM (
                        SELECT "TenantId", "NormalizedName"
                        FROM "{_schema.Schema}"."Labels"
                        GROUP BY "TenantId", "NormalizedName"
                        HAVING COUNT(*) > 1
                    );

                    IF duplicate_count > 0 THEN
                        SELECT LISTAGG('(' || NVL("TenantId", '<null>') || ', ' || "NormalizedName" || ')', '; ' ON OVERFLOW TRUNCATE)
                            WITHIN GROUP (ORDER BY "NormalizedName")
                        INTO duplicate_keys
                        FROM (
                            SELECT "TenantId", "NormalizedName"
                            FROM "{_schema.Schema}"."Labels"
                            GROUP BY "TenantId", "NormalizedName"
                            HAVING COUNT(*) > 1
                        );

                        RAISE_APPLICATION_ERROR(
                            -20001,
                            SUBSTR(
                                'Cannot create unique index IX_Label_TenantId_NormalizedName because leftover duplicate (TenantId, NormalizedName) rows exist. Operators must resolve leftover (TenantId, NormalizedName) rows before upgrade. Duplicate keys: ' || duplicate_keys,
                                1,
                                2048
                            )
                        );
                    END IF;
                END;
                """);

            // Name/NormalizedName are not truncated; list over-length Ids and abort before ALTER.
            migrationBuilder.Sql($"""
                DECLARE
                    over_length_ids VARCHAR2(4000);
                    over_length_count NUMBER;
                BEGIN
                    SELECT COUNT(*)
                    INTO over_length_count
                    FROM "{_schema.Schema}"."Labels"
                    WHERE LENGTH("Name") > 255 OR LENGTH("NormalizedName") > 255;

                    IF over_length_count > 0 THEN
                        SELECT LISTAGG("Id", ', ' ON OVERFLOW TRUNCATE) WITHIN GROUP (ORDER BY "Id")
                        INTO over_length_ids
                        FROM "{_schema.Schema}"."Labels"
                        WHERE LENGTH("Name") > 255 OR LENGTH("NormalizedName") > 255;

                        RAISE_APPLICATION_ERROR(
                            -20002,
                            SUBSTR(
                                'Cannot alter Labels.Name / Labels.NormalizedName to NVARCHAR2(255) because leftover rows exceed 255 characters. Operators must shorten or remove those rows before upgrade. Over-length Ids: ' || over_length_ids,
                                1,
                                2048
                            )
                        );
                    END IF;
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Labels",
                type: "NVARCHAR2(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Labels",
                type: "NVARCHAR2(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                schema: _schema.Schema,
                table: "Labels",
                type: "NVARCHAR2(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.CreateIndex(
                name: "IX_Label_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Labels",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");
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
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                schema: _schema.Schema,
                table: "Labels",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Labels",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(255)",
                oldMaxLength: 255);
        }
    }
}
