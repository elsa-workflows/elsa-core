using Elsa.Persistence.VNext.Sqlite;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Elsa.UserTasks.Persistence.VNext;
using Elsa.UserTasks.Persistence.VNext.Repositories;
using Microsoft.Data.Sqlite;

namespace Elsa.UserTasks.Persistence.ConformanceTests.Providers;

/// <summary>
/// The document-store provider, over the SQLite document store so it runs in CI without a container.
/// VNext ships a repository only, so the guest-session and outbox suites deliberately do not run here;
/// the coverage report states that rather than leaving it to be inferred from an absent test class.
/// </summary>
public sealed class VNextUserTaskStoreFixture : UserTaskStoreFixture
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly SqliteDocumentStore _store;
    private readonly VNextUserTaskRepository _repository;

    public VNextUserTaskStoreFixture() : base(ConformanceProviders.VNext)
    {
        _store = new(_connection, new UserTaskPersistenceSchemaProvider().DescribeSchema());
        _repository = new(_store);
    }

    public override IUserTaskRepository Repository => _repository;

    // One in-memory SQLite connection backs the document store, so both handles address the same data.
    public override IUserTaskRepository CreateSecondRepository() => new VNextUserTaskRepository(_store);

    protected override async Task ActivateCoreAsync()
    {
        await _connection.OpenAsync();
        await _store.MaterializeAsync();
    }

    protected override async Task DisposeCoreAsync() => await _connection.DisposeAsync();
}
