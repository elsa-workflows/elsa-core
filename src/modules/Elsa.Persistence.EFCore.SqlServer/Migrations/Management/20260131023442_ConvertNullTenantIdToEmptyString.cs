using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.SqlServer.Migrations.Management
{
    /// <inheritdoc />
    public partial class ConvertNullTenantIdToEmptyString : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public ConvertNullTenantIdToEmptyString(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert null TenantId values to empty string for default tenant entities
            // This aligns with ADR-0008 (empty string = default tenant) and ADR-0009 (null = tenant-agnostic)
            // All existing null values are assumed to be default tenant data from before the tenant-agnostic feature was introduced
            
            migrationBuilder.Sql($@"
                UPDATE [{_schema.Schema}].[WorkflowDefinitions] 
                SET TenantId = '' 
                WHERE TenantId IS NULL
            ");
            
            migrationBuilder.Sql($@"
                UPDATE [{_schema.Schema}].[WorkflowInstances] 
                SET TenantId = '' 
                WHERE TenantId IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert empty string TenantId values back to null
            // Note: This may cause issues with the tenant-agnostic feature if run after new tenant-agnostic entities are created
            
            migrationBuilder.Sql($@"
                UPDATE [{_schema.Schema}].[WorkflowDefinitions] 
                SET TenantId = NULL 
                WHERE TenantId = ''
            ");
            
            migrationBuilder.Sql($@"
                UPDATE [{_schema.Schema}].[WorkflowInstances] 
                SET TenantId = NULL 
                WHERE TenantId = ''
            ");
        }
    }
}
