using System.Data.Common;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows.Management.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// #8994: Oracle ExecuteUpdate of a captured <c>""</c> becomes <c>TenantId = :p</c> with a null bind.
/// The store must emit <c>IS NULL</c> so a second IncrementAsync("") updates generation 1 → 2.
/// </summary>
public class OracleDefaultTenantIncrementTests
{
    [Fact]
    public async Task IncrementAsync_DefaultTenantTwice_ReachesGenerationTwo_WithOracleStyleNullTenant()
    {
        await using var fixture = await IncrementFixture.CreateOracleStyleAsync();
        var store = fixture.Store;
        var interceptor = fixture.Interceptor;

        Assert.Equal(1, await store.IncrementAsync(Tenant.DefaultTenantId));
        Assert.Equal(2, await store.IncrementAsync(string.Empty));
        Assert.Equal(2, await store.GetGenerationAsync(Tenant.DefaultTenantId));

        Assert.Equal(1, await store.IncrementAsync("acme"));
        Assert.Equal(2, await store.IncrementAsync("acme"));
        Assert.Equal(2, await store.GetGenerationAsync("acme"));
        Assert.Equal(2, await store.GetGenerationAsync(Tenant.DefaultTenantId));

        var updates = interceptor.Commands
            .Where(sql => sql.Contains("UPDATE", StringComparison.OrdinalIgnoreCase)
                          && sql.Contains("WorkflowDefinitionRegistryGenerations", StringComparison.Ordinal))
            .ToArray();

        Assert.Contains(updates, sql => sql.Contains("IS NULL", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(updates, sql =>
            sql.Contains("TenantId", StringComparison.Ordinal)
            && !sql.Contains("IS NULL", StringComparison.OrdinalIgnoreCase));

        var lookups = interceptor.Commands
            .Where(sql => sql.Contains("SELECT", StringComparison.OrdinalIgnoreCase)
                          && sql.Contains("WorkflowDefinitionRegistryGenerations", StringComparison.Ordinal))
            .ToArray();

        Assert.Contains(lookups, sql => sql.Contains("IS NULL", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task IncrementAsync_DefaultTenantTwice_StillWorksOnSqliteWithoutConverter()
    {
        await using var fixture = await IncrementFixture.CreateNativeSqliteAsync();

        Assert.Equal(1, await fixture.Store.IncrementAsync(Tenant.DefaultTenantId));
        Assert.Equal(2, await fixture.Store.IncrementAsync(Tenant.DefaultTenantId));
        Assert.Equal(2, await fixture.Store.GetGenerationAsync(string.Empty));
    }

    private sealed class IncrementFixture : IAsyncDisposable
    {
        private IncrementFixture(
            SqliteConnection connection,
            ManagementElsaDbContext dbContext,
            EFCoreWorkflowDefinitionRegistryGenerationStore store,
            CommandCaptureInterceptor interceptor)
        {
            _connection = connection;
            _dbContext = dbContext;
            Store = store;
            Interceptor = interceptor;
        }

        private readonly SqliteConnection _connection;
        private readonly ManagementElsaDbContext _dbContext;

        public EFCoreWorkflowDefinitionRegistryGenerationStore Store { get; }
        public CommandCaptureInterceptor Interceptor { get; }

        public static Task<IncrementFixture> CreateOracleStyleAsync() =>
            CreateAsync(elsaOptions =>
            {
                elsaOptions.ConfigureModel<ManagementElsaDbContext>(modelBuilder =>
                {
                    modelBuilder.Entity<WorkflowDefinitionRegistryGeneration>(builder =>
                    {
                        builder.Property<long>("Id").ValueGeneratedOnAdd();
                        builder.HasKey("Id");
                        builder.Property(x => x.TenantId)
                            .IsRequired(false)
                            .HasConversion(
                                value => string.IsNullOrEmpty(value) ? null : value,
                                value => value ?? string.Empty);
                    });
                });
            });

        public static Task<IncrementFixture> CreateNativeSqliteAsync() => CreateAsync(_ => { });

        private static async Task<IncrementFixture> CreateAsync(Action<ElsaDbContextOptions> configureElsa)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var interceptor = new CommandCaptureInterceptor();
            var elsaOptions = new ElsaDbContextOptions();
            configureElsa(elsaOptions);

            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<ManagementElsaDbContext>();
                optionsBuilder
                    .EnableServiceProviderCaching(false)
                    .UseElsaDbContextOptions(elsaOptions)
                    .UseSqlite(connection)
                    .AddInterceptors(interceptor);

                var serviceProvider = new ServiceCollection().BuildServiceProvider();
                var dbContext = new ManagementElsaDbContext(optionsBuilder.Options, serviceProvider);
                await dbContext.Database.EnsureCreatedAsync();
                interceptor.Commands.Clear();

                return new IncrementFixture(
                    connection,
                    dbContext,
                    new EFCoreWorkflowDefinitionRegistryGenerationStore(dbContext),
                    interceptor);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _dbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return new ValueTask<InterceptionResult<int>>(result);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return new ValueTask<InterceptionResult<DbDataReader>>(result);
        }
    }
}
