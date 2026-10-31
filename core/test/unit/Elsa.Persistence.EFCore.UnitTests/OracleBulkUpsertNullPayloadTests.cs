using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.UnitTests;

public class OracleBulkUpsertNullPayloadTests
{
    [Theory]
    [InlineData("NCLOB", "TO_NCLOB({0})", false)]
    [InlineData("NCLOB", "TO_NCLOB({0})", true)]
    [InlineData("CLOB", "TO_CLOB({0})", false)]
    [InlineData("CLOB", "TO_CLOB({0})", true)]
    [InlineData("JSON", "TO_NCLOB({0})", false)]
    [InlineData("JSON", "TO_NCLOB({0})", true)]
    [InlineData("VARCHAR2(2000)", "CAST({0} AS VARCHAR2(2000))", false)]
    [InlineData("VARCHAR2(2000)", "CAST({0} AS VARCHAR2(2000))", true)]
    [InlineData("NCHAR(20)", "CAST({0} AS NCHAR(20))", false)]
    [InlineData("NCHAR(20)", "CAST({0} AS NCHAR(20))", true)]
    public void GenerateOracleUpsert_NormalizesTextPayloadTypesForEveryRow(string columnType, string expression, bool nullsFirst)
    {
        using var context = CreateContext(columnType);
        var missing = new TextPayload { Id = "missing" };
        var populated = new TextPayload { Id = "populated", Payload = "{\"value\":1}" };
        var rows = nullsFirst ? new[] { missing, populated } : new[] { populated, missing };

        var (sql, parameters) = BulkUpsertExtensions.GenerateOracleUpsert(context, rows, row => row.Id);

        Assert.Contains(expression.Replace("{0}", "{1}") + " AS \"Payload\"", sql);
        Assert.Contains(expression.Replace("{0}", "{3}") + " AS \"Payload\"", sql);
        Assert.Equal(4, parameters.Length);
        Assert.Null(parameters[nullsFirst ? 1 : 3]);
        Assert.Equal(populated.Payload, parameters[nullsFirst ? 3 : 1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenerateOracleUpsert_NormalizesBinaryPayloadTypesForEveryRow(bool nullsFirst)
    {
        using var context = CreateContext("BLOB");
        var missing = new BinaryPayload { Id = "missing" };
        var populated = new BinaryPayload { Id = "populated", Payload = new byte[] { 1, 2, 3 } };
        var rows = nullsFirst ? new[] { missing, populated } : new[] { populated, missing };

        var (sql, parameters) = BulkUpsertExtensions.GenerateOracleUpsert(context, rows, row => row.Id);

        Assert.Contains("TO_BLOB({1}) AS \"Payload\"", sql);
        Assert.Contains("TO_BLOB({3}) AS \"Payload\"", sql);
        Assert.Equal(4, parameters.Length);
        Assert.Null(parameters[nullsFirst ? 1 : 3]);
        Assert.Same(populated.Payload, parameters[nullsFirst ? 3 : 1]);
    }

    private static PayloadContext CreateContext(string columnType) => new(
        new DbContextOptionsBuilder<PayloadContext>().UseOracle("Data Source=unused").EnableServiceProviderCaching(false).Options,
        columnType);

    private sealed class TextPayload
    {
        public string Id { get; set; } = null!;
        public string? Payload { get; set; }
    }

    private sealed class BinaryPayload
    {
        public string Id { get; set; } = null!;
        public byte[]? Payload { get; set; }
    }

    private sealed class PayloadContext(DbContextOptions<PayloadContext> options, string columnType) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            if (columnType == "BLOB")
            {
                var entity = builder.Entity<BinaryPayload>();
                entity.Property(row => row.Id).HasColumnType("NVARCHAR2(450)");
                entity.Property(row => row.Payload).HasColumnType(columnType);
            }
            else
            {
                var entity = builder.Entity<TextPayload>();
                entity.Property(row => row.Id).HasColumnType("NVARCHAR2(450)");
                entity.Property(row => row.Payload).HasColumnType(columnType);
            }
        }
    }
}
