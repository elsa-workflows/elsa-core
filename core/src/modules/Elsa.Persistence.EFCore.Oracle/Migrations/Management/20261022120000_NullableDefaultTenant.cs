using Elsa.Persistence.EFCore.Oracle;
using Microsoft.EntityFrameworkCore.Migrations;
using Oracle.EntityFrameworkCore.Metadata;

#nullable disable

namespace Elsa.Persistence.EFCore.Oracle.Migrations.Management
{
    /// <inheritdoc />
    public partial class NullableDefaultTenant : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public NullableDefaultTenant(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TenantId is the PK of this table. Oracle forbids NULL in a primary key and stores '' as
            // NULL, so the default tenant (Tenant.DefaultTenantId = "") cannot be inserted. Replace the
            // PK with a shadow identity column and keep one row per tenant via NVL(TenantId, CHR(1)).
            // This is a new migration so a 3.10 preview that already applied #8391 still gets the ALTER.
            migrationBuilder.AddColumn<long>(
                name: "Id",
                schema: _schema.Schema,
                table: "WorkflowDefinitionRegistryGenerations",
                type: "NUMBER(19)",
                nullable: false)
                .Annotation("Oracle:ValueGenerationStrategy", OracleValueGenerationStrategy.IdentityColumn);

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkflowDefinitionRegistryGenerations",
                schema: _schema.Schema,
                table: "WorkflowDefinitionRegistryGenerations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkflowDefinitionRegistryGenerations",
                schema: _schema.Schema,
                table: "WorkflowDefinitionRegistryGenerations",
                column: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionRegistryGenerations",
                type: "NVARCHAR2(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)",
                oldMaxLength: 450);

            // CHR(1) is not a tenant id. NVL maps Oracle's NULL (from '') onto one index key so two
            // default-tenant rows collide. The EF snapshot cannot express NVL.
            MigrationHelper.CreateNvlUniqueIndexIfMissing(
                migrationBuilder,
                _schema,
                "IX_WorkflowDefinitionRegistryGenerations_TenantId",
                "WorkflowDefinitionRegistryGenerations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Oracle commits DROP INDEX. If this ran first, a later ORA-20006 would leave the
            // migration applied with no NVL unique index, so check for NULL TenantId rows first.
            MigrationHelper.EnsureNoNullValues(
                migrationBuilder,
                _schema,
                "WorkflowDefinitionRegistryGenerations",
                "TenantId");

            MigrationHelper.DropIndexIfPresent(
                migrationBuilder,
                _schema,
                "IX_WorkflowDefinitionRegistryGenerations_TenantId");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkflowDefinitionRegistryGenerations",
                schema: _schema.Schema,
                table: "WorkflowDefinitionRegistryGenerations");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionRegistryGenerations",
                type: "NVARCHAR2(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkflowDefinitionRegistryGenerations",
                schema: _schema.Schema,
                table: "WorkflowDefinitionRegistryGenerations",
                column: "TenantId");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: _schema.Schema,
                table: "WorkflowDefinitionRegistryGenerations");
        }
    }
}
