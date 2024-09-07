using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EntityFrameworkCore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class V3_3 : Migration
    {
        private readonly Elsa.EntityFrameworkCore.Common.Contracts.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_3(Elsa.EntityFrameworkCore.Common.Contracts.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Secrets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SecretId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    EncryptedValue = table.Column<string>(type: "TEXT", nullable: false),
                    IV = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptionKeyId = table.Column<string>(type: "TEXT", nullable: false),
                    Algorithm = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RotationPolicy = table.Column<string>(type: "TEXT", nullable: true),
                    LastAccessedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Secrets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Secret_Algorithm",
                table: "Secrets",
                column: "Algorithm");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_EncryptionKeyId",
                table: "Secrets",
                column: "EncryptionKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_ExpiresAt",
                table: "Secrets",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_LastAccessedAt",
                table: "Secrets",
                column: "LastAccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_Name",
                table: "Secrets",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_Status",
                table: "Secrets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_TenantId",
                table: "Secrets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_Type",
                table: "Secrets",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_Version",
                table: "Secrets",
                column: "Version");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Secrets");
        }
    }
}
