using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// A published→draft compare-and-swap that loses the <c>IsLatest</c> race must be Conflict,
/// not a unique-key failure on <c>(DefinitionId, Version)</c>.
/// </summary>
public class WorkflowDefinitionStoreCompareAndSwapTests
{
    [Fact(DisplayName = "A published→draft loser whose loaded row is no longer IsLatest is Conflict, not a unique-key failure")]
    public async Task TryUpdateLatestAsync_WhenPublishedRowIsNoLongerLatest_ReturnsConflict()
    {
        await using var harness = await EfWorkflowDefinitionStoreHarness.CreateAsync();
        var store = harness.Store;

        var published = Definition("def-1", "id-1", name: "Published", stringData: "graph-v1");
        published.IsPublished = true;
        await store.SaveAsync(published);

        var winner = await store.TryUpdateLatestAsync(
            LatestOf("def-1"),
            _ => true,
            loaded => NewDraftFrom(loaded, "id-2", "winner-draft"));

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Updated, winner.Outcome);

        var loser = await store.TryUpdateLatestAsync(
            new WorkflowDefinitionFilter { Id = published.Id },
            _ => true,
            loaded => NewDraftFrom(loaded, "id-3", "should-not-be-saved"));

        Assert.Equal(WorkflowDefinitionUpdateOutcome.Conflict, loser.Outcome);
        Assert.Null(loser.Definition);

        var stored = await store.FindAsync(LatestOf("def-1"));
        Assert.Equal("id-2", stored!.Id);
        Assert.Equal("winner-draft", stored.StringData);
    }

    private static WorkflowDefinitionFilter LatestOf(string definitionId) =>
        WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Latest).ToFilter();

    private static WorkflowDefinition Definition(string definitionId, string id, string name, string stringData) =>
        new()
        {
            Id = id,
            DefinitionId = definitionId,
            Name = name,
            StringData = stringData,
            Version = 1,
            IsLatest = true,
            MaterializerName = "Json"
        };

    private static WorkflowDefinition NewDraftFrom(WorkflowDefinition published, string id, string stringData)
    {
        var draft = published.ShallowClone();
        draft.Id = id;
        draft.Version = published.Version + 1;
        draft.IsLatest = true;
        draft.IsPublished = false;
        draft.StringData = stringData;
        return draft;
    }

    private sealed class EfWorkflowDefinitionStoreHarness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _services;

        private EfWorkflowDefinitionStoreHarness(SqliteConnection connection, ServiceProvider services, EFCoreWorkflowDefinitionStore store)
        {
            _connection = connection;
            _services = services;
            Store = store;
        }

        public EFCoreWorkflowDefinitionStore Store { get; }

        public static async Task<EfWorkflowDefinitionStoreHarness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IPayloadSerializer, StablePayloadSerializer>()
                .AddDbContextFactory<ManagementElsaDbContext>((_, options) =>
                {
                    options.UseSqlite(connection);
                    options.UseElsaDbContextOptions(new ElsaDbContextOptions { SchemaName = "main" });
                })
                .BuildServiceProvider();

            var factory = services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>();
            await using (var dbContext = await factory.CreateDbContextAsync())
                await dbContext.Database.EnsureCreatedAsync();

            var entityStore = new EntityStore<ManagementElsaDbContext, WorkflowDefinition>(factory, services);
            var store = new EFCoreWorkflowDefinitionStore(
                entityStore,
                services.GetRequiredService<IPayloadSerializer>(),
                NullLogger<EFCoreWorkflowDefinitionStore>.Instance);

            return new EfWorkflowDefinitionStoreHarness(connection, services, store);
        }

        public async ValueTask DisposeAsync()
        {
            await _services.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    /// <summary>
    /// Keeps the hidden <c>Data</c> column stable so the compare-and-swap WHERE is about
    /// identity and <c>IsLatest</c>, not serializer noise.
    /// </summary>
    private sealed class StablePayloadSerializer : IPayloadSerializer
    {
        private const string Payload = "{}";

        public string Serialize(object payload) => Payload;
        public JsonElement SerializeToElement(object payload) => JsonDocument.Parse(Payload).RootElement.Clone();
        public object Deserialize(string serializedData) => new();
        public object Deserialize(string serializedData, Type type) => Activator.CreateInstance(type)!;
        public object Deserialize(JsonElement serializedData) => new();
        public T Deserialize<T>(string serializedData) => (T)Activator.CreateInstance(typeof(T))!;
        public T Deserialize<T>(JsonElement serializedData) => (T)Activator.CreateInstance(typeof(T))!;
        public JsonSerializerOptions GetOptions() => new();
    }
}
