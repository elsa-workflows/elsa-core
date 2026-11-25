using System;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EFCore.Oracle.Migrations.Secrets
{
    /// <inheritdoc />
    public partial class ManagedSecretOwnership : Migration
    {
        private readonly IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public ManagedSecretOwnership(IElsaDbContextSchema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Secret_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Secrets");

            migrationBuilder.AddColumn<string>(
                name: "ManagedGenerationId",
                schema: _schema.Schema,
                table: "Secrets",
                type: "NVARCHAR2(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagedOwnerId",
                schema: _schema.Schema,
                table: "Secrets",
                type: "NVARCHAR2(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Secret_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Secrets",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This migration is irreversible because dropping ownership markers could make lifecycle credential material mutable.");
        }
    }
}
