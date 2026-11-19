using System.Transactions;
using Elsa.Persistence.EFCore.Extensions;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Elsa.Persistence.EFCore.MySql.UnitTests;

public sealed class MySqlBulkUpsertTests
{
    private const string ConnectionStringVariable = "ELSA_MYSQL_BULK_UPSERT_TEST_CONNECTION";

    [NativeMySqlFact]
    public async Task InsertOrUpdateUsesOnlyTheRequestedKey()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable)!;
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;
        await using var dbContext = new TestDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var key = Guid.NewGuid().ToString("N");
        var original = new TestRow { Id = $"{key}-id", Reference = $"{key}-ref", Payload = "original" };

        await dbContext.BulkUpsertAsync([original], row => row.Id, batchSize: 50);
        original.Payload = "updated";
        await dbContext.BulkUpsertAsync([original], row => row.Id, batchSize: 50);
        await dbContext.BulkUpsertAsync([original], row => row.Id, batchSize: 50);

        using (var ambient = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            original.Payload = "ambient";
            await dbContext.BulkUpsertAsync([original], row => row.Id, batchSize: 50);
            ambient.Complete();
        }

        var another = new TestRow { Id = $"{key}-second-id", Reference = $"{key}-second-ref", Payload = "second" };
        var duplicateReference = new TestRow { Id = $"{key}-other-id", Reference = original.Reference, Payload = "different" };
        var error = await Assert.ThrowsAsync<MySqlException>(() =>
            dbContext.BulkUpsertAsync([another, duplicateReference], row => row.Id, batchSize: 50));
        Assert.Equal(1062, error.Number);

        var persisted = Assert.Single(await dbContext.Rows.AsNoTracking().Where(row => row.Id.StartsWith(key)).ToListAsync());
        Assert.Equal(original.Id, persisted.Id);
        Assert.Equal(original.Reference, persisted.Reference);
        Assert.Equal("ambient", persisted.Payload);
    }

    public sealed class NativeMySqlFactAttribute : FactAttribute
    {
        public NativeMySqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringVariable)))
                Skip = $"Set {ConnectionStringVariable} to a disposable MySQL database.";
        }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestRow> Rows => Set<TestRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var rows = modelBuilder.Entity<TestRow>();
            rows.ToTable("NativeBulkUpsertTestRows");
            rows.Property(row => row.Id).HasMaxLength(80);
            rows.Property(row => row.Reference).HasMaxLength(80);
            rows.HasIndex(row => row.Reference).IsUnique();
        }
    }

    private sealed class TestRow
    {
        public string Id { get; set; } = null!;
        public string Reference { get; set; } = null!;
        public string Payload { get; set; } = null!;
    }
}
