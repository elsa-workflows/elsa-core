using System;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EFCore.MySql.Migrations.Secrets
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
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ManagedOwnerId",
                schema: _schema.Schema,
                table: "Secrets",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This migration is irreversible because dropping ownership markers could make lifecycle credential material mutable.");
        }
    }
}
