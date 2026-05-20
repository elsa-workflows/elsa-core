using System.Text.Json;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Models;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class KeyValueWorkflowDispatchOutboxStoreTests
{
    private readonly IKeyValueStore _keyValueStore = Substitute.For<IKeyValueStore>();
    private readonly TestPayloadSerializer _payloadSerializer = new();
    private readonly KeyValueWorkflowDispatchOutboxStore _store;

    public KeyValueWorkflowDispatchOutboxStoreTests()
    {
        _store = new(_keyValueStore, _payloadSerializer);
    }

    [Fact]
    public async Task FindManyAsync_ReturnsOldestItemsBeforeApplyingLimit()
    {
        var newest = CreateItem("newest", new DateTimeOffset(2026, 5, 20, 12, 2, 0, TimeSpan.Zero));
        var oldest = CreateItem("oldest", new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));
        var middle = CreateItem("middle", new DateTimeOffset(2026, 5, 20, 12, 1, 0, TimeSpan.Zero));
        _payloadSerializer.Items = new[] { newest, oldest, middle }.ToDictionary(x => x.Id);
        _keyValueStore.FindManyAsync(Arg.Any<KeyValueFilter>(), Arg.Any<CancellationToken>())
            .Returns(_payloadSerializer.Items.Keys.Select(x => new SerializedKeyValuePair { Key = x, SerializedValue = x }));

        var result = (await _store.FindManyAsync(2)).ToList();

        Assert.Equal(["oldest", "middle"], result.Select(x => x.Id));
        await _keyValueStore.Received(1).FindManyAsync(
            Arg.Is<KeyValueFilter>(x => x.Take == null),
            Arg.Any<CancellationToken>());
    }

    private static WorkflowDispatchOutboxItem CreateItem(string id, DateTimeOffset createdAt)
    {
        return new()
        {
            Id = id,
            OwnerWorkflowInstanceId = "parent-1",
            Kind = WorkflowDispatchOutboxItemKind.WorkflowDefinition,
            WorkflowDefinitionCommand = new DispatchWorkflowDefinitionCommand("definition-version-1"),
            CreatedAt = createdAt
        };
    }

    private class TestPayloadSerializer : IPayloadSerializer
    {
        public IDictionary<string, WorkflowDispatchOutboxItem> Items { get; set; } = new Dictionary<string, WorkflowDispatchOutboxItem>();

        public string Serialize(object payload) => ((WorkflowDispatchOutboxItem)payload).Id;

        public JsonElement SerializeToElement(object payload) => throw new NotSupportedException();

        public object Deserialize(string serializedData) => Deserialize<WorkflowDispatchOutboxItem>(serializedData);

        public object Deserialize(string serializedData, Type type) => Deserialize(serializedData);

        public object Deserialize(JsonElement serializedData) => throw new NotSupportedException();

        public T Deserialize<T>(string serializedData) => (T)(object)Items[serializedData];

        public T Deserialize<T>(JsonElement serializedData) => throw new NotSupportedException();

        public JsonSerializerOptions GetOptions() => new();
    }
}
