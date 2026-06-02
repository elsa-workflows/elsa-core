using System.Data.Common;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Relational.IntegrationTests.Contracts;

namespace Elsa.ModularPersistence.Relational.IntegrationTests.Providers;

public abstract class RelationalProviderFixtureBase : IRelationalProviderFixture
{
    public abstract string ProviderName { get; }

    public abstract bool IsAvailable { get; }

    public abstract ValueTask ResetAsync();

    public abstract ValueTask MaterializeAsync(StorageManifestDescriptor manifest);

    public abstract IDocumentStore CreateStore(StorageManifestDescriptor manifest);

    public abstract ValueTask<bool> TableExistsAsync(string tableName);

    public abstract ValueTask<bool> IndexExistsAsync(string indexName);

    public abstract ValueTask<int> CountIndexRowsAsync(DocumentKey key);

    public abstract ValueTask<IReadOnlyCollection<(string SchemaName, string Version)>> ReadSchemaHistoryAsync();

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    protected static void AddKeyParameters(DbCommand command, DocumentKey key)
    {
        AddParameter(command, "@Id", key.Id);
        AddParameter(command, "@Type", key.Type);
        AddParameter(command, "@TenantId", key.TenantId ?? "");
    }
}
