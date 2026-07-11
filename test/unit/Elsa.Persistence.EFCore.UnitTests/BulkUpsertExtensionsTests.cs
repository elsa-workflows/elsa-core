using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.UnitTests;

public class BulkUpsertExtensionsTests
{
    [Fact]
    public void GenerateOracleUpsert_ProducesQuotedMergeWithNvarcharCasts()
    {
        using var dbContext = CreateDbContext();
        var entities = new List<TestEntity>
        {
            new() { Id = "first", Name = "First", Count = 1 },
            new() { Id = "second", Name = null, Count = 2 }
        };

        var (sql, parameters) = BulkUpsertExtensions.GenerateOracleUpsert(dbContext, entities, x => x.Id);

        Assert.Contains("MERGE INTO \"Elsa\".\"ActivityExecutionRecords\" Target", sql);
        Assert.Contains("CAST({0} AS NVARCHAR2(450)) AS \"RecordId\"", sql);
        Assert.Contains("CAST({2} AS NVARCHAR2(2000)) AS \"DisplayName\"", sql);
        Assert.Contains("CAST({5} AS NVARCHAR2(2000)) AS \"DisplayName\"", sql);
        Assert.Contains("FROM DUAL UNION ALL SELECT", sql);
        Assert.Contains("Target.\"RecordId\" = Source.\"RecordId\"", sql);
        Assert.Contains("INSERT (\"RecordId\", \"Count\", \"DisplayName\")", sql);
        Assert.Contains("VALUES (Source.\"RecordId\", Source.\"Count\", Source.\"DisplayName\")", sql);
        Assert.DoesNotContain("CAST({1} AS NUMBER", sql);

        var updateClause = sql[sql.IndexOf("WHEN MATCHED", StringComparison.Ordinal)..sql.IndexOf("WHEN NOT MATCHED", StringComparison.Ordinal)];
        Assert.DoesNotContain("Target.\"RecordId\" = Source.\"RecordId\"", updateClause);
        Assert.Contains("Target.\"DisplayName\" = Source.\"DisplayName\"", updateClause);
        Assert.Equal(new object?[] { "first", 1, "First", "second", 2, null }, parameters);
    }

    private static TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseOracle("Data Source=unused")
            .Options;
        return new TestDbContext(options);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<TestEntity>();
            entity.ToTable("ActivityExecutionRecords", "Elsa");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("RecordId").HasColumnType("NVARCHAR2(450)");
            entity.Property(x => x.Count).HasColumnType("NUMBER(10)");
            entity.Property(x => x.Name).HasColumnName("DisplayName").HasColumnType("NVARCHAR2(2000)");
        }
    }

    private sealed class TestEntity
    {
        public string Id { get; set; } = null!;
        public int Count { get; set; }
        public string? Name { get; set; }
    }
}
