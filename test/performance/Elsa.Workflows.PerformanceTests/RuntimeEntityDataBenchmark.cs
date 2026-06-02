using BenchmarkDotNet.Attributes;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Queries;
using Elsa.ModularPersistence.Runtime;

namespace Elsa.Workflows.PerformanceTests;

[Config(typeof(Config))]
public class RuntimeEntityDataBenchmark
{
    private readonly RuntimeStorageOperationContext _context = RuntimeStorageOperationContext.System;
    private InMemoryRuntimeStorageDefinitionStore _definitionStore = null!;
    private RuntimeEntityDataService _service = null!;
    private int _counter;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _definitionStore = new InMemoryRuntimeStorageDefinitionStore();
        await _definitionStore.SaveAsync(new RuntimeStorageDefinition(
            "customers",
            "runtime.customers",
            "Customers",
            [
                new RuntimeStorageFieldDefinition("Id", StorageFieldType.String, true),
                new RuntimeStorageFieldDefinition("Email", StorageFieldType.String, true),
                new RuntimeStorageFieldDefinition("Priority", StorageFieldType.Int32)
            ],
            [
                new RuntimeStorageIndexDefinition("IX_Customers_Email", ["Email"]),
                new RuntimeStorageIndexDefinition("IX_Customers_Priority", ["Priority"])
            ],
            ["read:customers"])
        {
            State = RuntimeStorageDefinitionState.Published
        });
        _service = new RuntimeEntityDataService(_definitionStore, new InMemoryRuntimeEntityDocumentStoreFactoryRegistry());

        for (var i = 0; i < 1000; i++)
            await _service.CreateAsync("customers", new RuntimeEntitySaveRequest($"seed-{i}", CreatePayload($"seed-{i}", $"seed-{i}@example.com", i)), _context);
    }

    [Benchmark]
    public async ValueTask<RuntimeEntityRecord> Create()
    {
        var index = Interlocked.Increment(ref _counter);
        return await _service.CreateAsync("customers", new RuntimeEntitySaveRequest($"created-{index}", CreatePayload($"created-{index}", $"created-{index}@example.com", index)), _context);
    }

    [Benchmark]
    public async ValueTask<RuntimeEntityRecord?> Read() =>
        await _service.GetAsync("customers", "seed-500", null, _context);

    [Benchmark]
    public async ValueTask<IReadOnlyCollection<RuntimeEntityRecord>> QueryByIndexedField() =>
        await _service.QueryAsync(
            "customers",
            new RuntimeEntityQueryRequest(
                [
                    new RuntimeEntityQueryFilter("Email", DocumentQueryFilterOperator.Equals, [DocumentQueryValue.String("seed-500@example.com")])
                ]),
            _context);

    [Benchmark]
    public async ValueTask<RuntimeEntityRecord> Update()
    {
        var id = $"update-{Interlocked.Increment(ref _counter)}";
        var created = await _service.CreateAsync("customers", new RuntimeEntitySaveRequest(id, CreatePayload(id, $"{id}@example.com", 1)), _context);
        return await _service.UpdateAsync("customers", new RuntimeEntitySaveRequest(id, CreatePayload(id, $"{id}@example.com", 2), ExpectedVersion: created.Version), _context);
    }

    private static string CreatePayload(string id, string email, int priority) =>
        $$"""{"Id":"{{id}}","Email":"{{email}}","Priority":{{priority}}}""";

}
