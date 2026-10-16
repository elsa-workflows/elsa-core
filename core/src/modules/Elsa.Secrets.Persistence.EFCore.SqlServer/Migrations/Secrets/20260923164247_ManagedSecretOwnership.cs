using System;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EFCore.SqlServer.Migrations.Secrets
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
            migrationBuilder.AddColumn<string>(
                name: "ManagedGenerationId",
                schema: _schema.Schema,
                table: "Secrets",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagedOwnerId",
                schema: _schema.Schema,
                table: "Secrets",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This migration is irreversible because dropping ownership markers could make lifecycle credential material mutable.");
        }
    }
}
