using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Oracle.Migrations.Runtime
{
    /// <inheritdoc />
    /// <remarks>
    /// V3_5 created <c>ActivityExecutionRecords.SerializedMetadata</c> as NVARCHAR2(2000). Later designers and
    /// the runtime snapshot pin the column as NCLOB (and <c>Configurations/Runtime.cs</c> does the same), but no
    /// <c>Up()</c> actually converted the database column. Oracle rejects an in-place
    /// <c>ALTER TABLE ... MODIFY</c> that changes a column's datatype to or from a LOB type (ORA-22858 /
    /// ORA-22859), so the conversion uses the add/copy/drop/rename sequence
    /// <see cref="MigrationHelper.ConvertColumnType"/> emits. Because Oracle commits DDL implicitly, a run that
    /// fails partway leaves its earlier statements applied; every step here is therefore guarded so that a
    /// re-run picks up where the failed one stopped instead of converting an already-converted column.
    /// </remarks>
    public partial class ActivityExecutionSerializedMetadataNclob : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public ActivityExecutionSerializedMetadataNclob(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ConvertSerializedMetadataToNclob(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The upgraded NCLOB column permits values longer than the NVARCHAR2(2000) the downgrade converts
            // back to. Copying such a value would silently truncate it, so the column is preflighted for
            // oversized values before any statement of the downgrade runs.
            MigrationHelper.EnsureLobLengthAtMost(migrationBuilder, _schema, "ActivityExecutionRecords", "SerializedMetadata", 2000);

            ConvertSerializedMetadataToNVarchar2(migrationBuilder);
        }

        private void ConvertSerializedMetadataToNclob(MigrationBuilder migrationBuilder)
        {
            MigrationHelper.ConvertColumnType(
                migrationBuilder,
                _schema,
                table: "ActivityExecutionRecords",
                column: "SerializedMetadata",
                fromDataType: "NVARCHAR2",
                toDataType: "NCLOB",
                toColumnDefinition: "NCLOB",
                copyExpression: source => $"TO_NCLOB({source})",
                notNull: false);
        }

        private void ConvertSerializedMetadataToNVarchar2(MigrationBuilder migrationBuilder)
        {
            MigrationHelper.ConvertColumnType(
                migrationBuilder,
                _schema,
                table: "ActivityExecutionRecords",
                column: "SerializedMetadata",
                fromDataType: "NCLOB",
                toDataType: "NVARCHAR2",
                toColumnDefinition: "NVARCHAR2(2000)",
                // DBMS_LOB.SUBSTR counts characters for an NCLOB and returns NVARCHAR2, so it neither
                // truncates a value that already passed the length preflight nor goes through the database
                // character set the way TO_CHAR would.
                copyExpression: source => $"DBMS_LOB.SUBSTR({source}, 2000, 1)",
                notNull: false);
        }
    }
}
