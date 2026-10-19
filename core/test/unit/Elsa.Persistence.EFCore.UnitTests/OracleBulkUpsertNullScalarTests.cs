using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.UnitTests;

public class OracleBulkUpsertNullScalarTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenerateOracleUpsert_CastsNullScalarsUsingColumnTypes(bool nullsFirst)
    {
        using var context = new ScalarContext(new DbContextOptionsBuilder<ScalarContext>().UseOracle("Data Source=unused").Options);
        var missing = new ScalarEntity { Id = "missing" };
        var populated = new ScalarEntity
        {
            Id = "populated",
            CompletedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Count = 7,
            Enabled = true,
            Payload = "text",
        };
        var rows = nullsFirst ? new[] { missing, populated } : new[] { populated, missing };

        var (sql, parameters) = BulkUpsertExtensions.GenerateOracleUpsert(context, rows, row => row.Id);

        var nullOffset = nullsFirst ? 0 : 5;
        var valueOffset = nullsFirst ? 5 : 0;
        Assert.Contains($"CAST({{{nullOffset + 1}}} AS TIMESTAMP(7) WITH TIME ZONE) AS \"CompletedAt\"", sql);
        Assert.Contains($"CAST({{{nullOffset + 2}}} AS NUMBER(10)) AS \"Count\"", sql);
        Assert.Contains($"CAST({{{nullOffset + 3}}} AS BOOLEAN) AS \"Enabled\"", sql);
        Assert.DoesNotContain($"CAST({{{valueOffset + 1}}} AS TIMESTAMP", sql);
        Assert.DoesNotContain($"CAST({{{valueOffset + 2}}} AS NUMBER", sql);
        Assert.DoesNotContain($"CAST({{{valueOffset + 3}}} AS BOOLEAN", sql);
        Assert.DoesNotContain(" AS NCLOB)", sql);
        Assert.Contains("FROM DUAL UNION ALL SELECT", sql);
        Assert.Null(parameters[nullOffset + 1]);
        Assert.Null(parameters[nullOffset + 2]);
        Assert.Null(parameters[nullOffset + 3]);
        Assert.Equal(populated.CompletedAt, parameters[valueOffset + 1]);
        Assert.Equal(populated.Count, parameters[valueOffset + 2]);
        Assert.Equal(populated.Enabled, parameters[valueOffset + 3]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenerateOracleUpsert_CastsConvertedStringsUsingProviderType(bool nullsFirst)
    {
        using var context = new ConvertedScalarContext(new DbContextOptionsBuilder<ConvertedScalarContext>().UseOracle("Data Source=unused").Options);
        var missing = new ConvertedScalarEntity { Id = "missing" };
        var populated = new ConvertedScalarEntity { Id = "populated", Status = CompletionStatus.Done };
        var rows = nullsFirst ? new[] { missing, populated } : new[] { populated, missing };

        var (sql, parameters) = BulkUpsertExtensions.GenerateOracleUpsert(context, rows, row => row.Id);

        Assert.Contains("CAST({1} AS VARCHAR2(32)) AS \"Status\"", sql);
        Assert.Contains("CAST({3} AS VARCHAR2(32)) AS \"Status\"", sql);
        Assert.Equal(
            nullsFirst
                ? new object?[] { "missing", null, "populated", "Done" }
                : new object?[] { "populated", "Done", "missing", null },
            parameters);
    }

    private enum CompletionStatus
    {
        Done
    }

    private sealed class ConvertedScalarEntity
    {
        public string Id { get; set; } = null!;
        public CompletionStatus? Status { get; set; }
    }

    private sealed class ConvertedScalarContext(DbContextOptions<ConvertedScalarContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            var entity = builder.Entity<ConvertedScalarEntity>();
            entity.Property(row => row.Id).HasColumnType("NVARCHAR2(450)");
            entity.Property(row => row.Status).HasConversion<string>().HasColumnType("VARCHAR2(32)");
        }
    }

    private sealed class ScalarEntity
    {
        public string Id { get; set; } = null!;
        public DateTimeOffset? CompletedAt { get; set; }
        public int? Count { get; set; }
        public bool? Enabled { get; set; }
        public string? Payload { get; set; }
    }

    private sealed class ScalarContext(DbContextOptions<ScalarContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            var entity = builder.Entity<ScalarEntity>();
            entity.Property(row => row.Id).HasColumnType("NVARCHAR2(450)");
            entity.Property(row => row.CompletedAt).HasColumnType("TIMESTAMP(7) WITH TIME ZONE");
            entity.Property(row => row.Count).HasColumnType("NUMBER(10)");
            entity.Property(row => row.Enabled).HasColumnType("BOOLEAN");
            entity.Property(row => row.Payload).HasColumnType("NCLOB");
        }
    }
}
