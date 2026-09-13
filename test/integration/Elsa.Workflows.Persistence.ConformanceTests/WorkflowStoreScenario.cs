using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Codecs;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Persistence.EFCore.Sqlite;
using Elsa.Tenants.Options;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Persistence.ConformanceTests;

/// <summary>
/// Holds one Memory or EF/SQLite set of management + runtime stores and the ambient tenant they read.
/// </summary>
public sealed class WorkflowStoreScenario(
    TestTenantAccessor tenantAccessor,
    IWorkflowDefinitionStore definitions,
    ITriggerStore triggers,
    IBookmarkStore bookmarks,
    IBookmarkQueueDeadLetterStore deadLetters,
    IActivityExecutionStore activityExecutions,
    IWorkflowExecutionLogStore executionLogs,
    Func<Func<Task>, Task> assertUniquenessConflictAsync,
    Func<ValueTask> disposeAsync) : IAsyncDisposable
{
    public TestTenantAccessor TenantAccessor { get; } = tenantAccessor;
    public IWorkflowDefinitionStore Definitions { get; } = definitions;
    public ITriggerStore Triggers { get; } = triggers;
    public IBookmarkStore Bookmarks { get; } = bookmarks;
    public IBookmarkQueueDeadLetterStore DeadLetters { get; } = deadLetters;
    public IActivityExecutionStore ActivityExecutions { get; } = activityExecutions;
    public IWorkflowExecutionLogStore ExecutionLogs { get; } = executionLogs;

    public IDisposable UseTenant(string tenantId) =>
        TenantAccessor.PushContext(tenantId == Tenant.DefaultTenantId
            ? Tenant.Default
            : new Tenant { Id = tenantId, Name = tenantId });

    public Task AssertUniquenessConflictAsync(Func<Task> operation) => assertUniquenessConflictAsync(operation);

    public ValueTask DisposeAsync() => disposeAsync();

    public static Task<WorkflowStoreScenario> CreateInMemoryAsync()
    {
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        return Task.FromResult(new WorkflowStoreScenario(
            tenantAccessor,
            new MemoryWorkflowDefinitionStore(new MemoryStore<WorkflowDefinition>(), tenantAccessor),
            new MemoryTriggerStore(new MemoryStore<StoredTrigger>(), tenantAccessor),
            new MemoryBookmarkStore(new MemoryStore<StoredBookmark>(), tenantAccessor),
            new MemoryBookmarkQueueDeadLetterStore(new MemoryStore<BookmarkQueueDeadLetterItem>()),
            new MemoryActivityExecutionStore(new MemoryStore<ActivityExecutionRecord>()),
            new MemoryWorkflowExecutionLogStore(new MemoryStore<WorkflowExecutionLogRecord>()),
            operation => Assert.ThrowsAsync<InvalidOperationException>(operation),
            () => ValueTask.CompletedTask));
    }

    public static async Task<WorkflowStoreScenario> CreateSqliteAsync()
    {
        var managementPath = Path.Combine(Path.GetTempPath(), $"elsa-workflow-management-conformance-{Guid.NewGuid():N}.db");
        var runtimePath = Path.Combine(Path.GetTempPath(), $"elsa-workflow-runtime-conformance-{Guid.NewGuid():N}.db");
        var tenantAccessor = new TestTenantAccessor("tenant-a");
        ServiceProvider? services = null;
        IServiceScope? scope = null;

        try
        {
            var migrationsAssembly = typeof(ManagementDbContextFactory).Assembly;
            services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<ITenantAccessor>(tenantAccessor)
                .AddSingleton<IPayloadSerializer, ConformancePayloadSerializer>()
                .AddSingleton<ISafeSerializer, ConformanceSafeSerializer>()
                .AddSingleton<ICompressionCodecResolver>(_ => new CompressionCodecResolver([new None()]))
                .Configure<TenantsOptions>(options => options.IsEnabled = true)
                .AddScoped<IEntitySavingHandler, ApplyTenantId>()
                .AddScoped<IEntityModelCreatingHandler, SetTenantIdFilter>()
                .AddSqliteEntityModelCreatingHandlers()
                .AddDbContextFactory<ManagementElsaDbContext>((_, builder) =>
                    builder.UseElsaSqlite(migrationsAssembly, $"Data Source={managementPath};Default Timeout=30"))
                .AddDbContextFactory<RuntimeElsaDbContext>((_, builder) =>
                    builder.UseElsaSqlite(migrationsAssembly, $"Data Source={runtimePath};Default Timeout=30"))
                .Decorate<IDbContextFactory<ManagementElsaDbContext>, TenantAwareDbContextFactory<ManagementElsaDbContext>>()
                .Decorate<IDbContextFactory<RuntimeElsaDbContext>, TenantAwareDbContextFactory<RuntimeElsaDbContext>>()
                .AddScoped<EntityStore<ManagementElsaDbContext, WorkflowDefinition>>()
                .AddScoped<EntityStore<RuntimeElsaDbContext, StoredTrigger>>()
                .AddScoped<Store<RuntimeElsaDbContext, StoredBookmark>>()
                .AddScoped<Store<RuntimeElsaDbContext, BookmarkQueueDeadLetterItem>>()
                .AddScoped<EntityStore<RuntimeElsaDbContext, ActivityExecutionRecord>>()
                .AddScoped<EntityStore<RuntimeElsaDbContext, WorkflowExecutionLogRecord>>()
                .AddScoped<EFCoreWorkflowDefinitionStore>()
                .AddScoped<EFCoreTriggerStore>()
                .AddScoped<EFCoreBookmarkStore>()
                .AddScoped<EFBookmarkQueueDeadLetterStore>()
                .AddScoped<EFCoreActivityExecutionStore>()
                .AddScoped<EFCoreWorkflowExecutionLogStore>()
                .BuildServiceProvider();

            await using (var management = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
                await management.Database.EnsureCreatedAsync();
            await using (var runtime = await services.GetRequiredService<IDbContextFactory<RuntimeElsaDbContext>>().CreateDbContextAsync())
                await runtime.Database.EnsureCreatedAsync();

            scope = services.CreateScope();
            var scoped = scope.ServiceProvider;

            return new(
                tenantAccessor,
                scoped.GetRequiredService<EFCoreWorkflowDefinitionStore>(),
                scoped.GetRequiredService<EFCoreTriggerStore>(),
                scoped.GetRequiredService<EFCoreBookmarkStore>(),
                scoped.GetRequiredService<EFBookmarkQueueDeadLetterStore>(),
                scoped.GetRequiredService<EFCoreActivityExecutionStore>(),
                scoped.GetRequiredService<EFCoreWorkflowExecutionLogStore>(),
                AssertSqliteUniquenessConflictAsync,
                async () =>
                {
                    scope.Dispose();
                    await services.DisposeAsync();
                    SqliteConnection.ClearAllPools();
                    File.Delete(managementPath);
                    File.Delete(runtimePath);
                });
        }
        catch
        {
            scope?.Dispose();
            if (services is not null)
                await services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(managementPath);
            File.Delete(runtimePath);
            throw;
        }
    }

    private static async Task AssertSqliteUniquenessConflictAsync(Func<Task> operation)
    {
        var exception = await Record.ExceptionAsync(operation);
        var sqliteException = exception switch
        {
            DbUpdateException { InnerException: SqliteException inner } => inner,
            SqliteException direct => direct,
            _ => throw new Xunit.Sdk.XunitException($"Expected a SQLite uniqueness violation, received {exception?.GetType().FullName ?? "no exception"}.")
        };

        Assert.Equal(19, sqliteException.SqliteErrorCode);
        Assert.Contains("UNIQUE constraint failed", sqliteException.Message, StringComparison.Ordinal);
    }

    private sealed class ConformancePayloadSerializer : IPayloadSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public string Serialize(object payload) => JsonSerializer.Serialize(payload, Options);
        public JsonElement SerializeToElement(object payload) => JsonSerializer.SerializeToElement(payload, Options);
        public object Deserialize(string serializedData) => JsonSerializer.Deserialize<object>(serializedData, Options)!;
        public object Deserialize(string serializedData, Type type) => JsonSerializer.Deserialize(serializedData, type, Options)!;
        public object Deserialize(JsonElement serializedData) => serializedData.Deserialize<object>(Options)!;
        public T Deserialize<T>(string serializedData) => JsonSerializer.Deserialize<T>(serializedData, Options)!;
        public T Deserialize<T>(JsonElement serializedData) => serializedData.Deserialize<T>(Options)!;
        public JsonSerializerOptions GetOptions() => Options;
    }

    private sealed class ConformanceSafeSerializer : ISafeSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public ValueTask<string> SerializeAsync(object? value, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Serialize(value));

        public ValueTask<JsonElement> SerializeToElementAsync(object? value, CancellationToken cancellationToken = default) =>
            new(SerializeToElement(value));

        public ValueTask<T> DeserializeAsync<T>(string json, CancellationToken cancellationToken = default) =>
            new(Deserialize<T>(json));

        public ValueTask<T> DeserializeAsync<T>(JsonElement element, CancellationToken cancellationToken = default) =>
            new(Deserialize<T>(element));

        public string Serialize(object? value) => JsonSerializer.Serialize(value, Options);
        public JsonElement SerializeToElement(object? value) => JsonSerializer.SerializeToElement(value, Options);
        public T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options)!;
        public T Deserialize<T>(JsonElement element) => element.Deserialize<T>(Options)!;
        public JsonSerializerOptions GetOptions() => Options;
    }
}
