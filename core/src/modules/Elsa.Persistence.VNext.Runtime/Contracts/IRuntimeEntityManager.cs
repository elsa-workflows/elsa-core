using Elsa.Persistence.VNext.Runtime.Models;

namespace Elsa.Persistence.VNext.Runtime.Contracts;

public interface IRuntimeEntityManager
{
    Task<RuntimeEntityDefinition> SaveDraftAsync(RuntimeEntityDefinition definition, CancellationToken cancellationToken = default);
    Task<RuntimeEntityDefinition?> GetDefinitionAsync(string name, CancellationToken cancellationToken = default);
    Task<RuntimeEntityDefinition> PublishAsync(string name, CancellationToken cancellationToken = default);
    Task<RuntimeEntityDefinition> RetireAsync(string name, CancellationToken cancellationToken = default);
    Task<RuntimeEntityInstance> SaveInstanceAsync(RuntimeEntityInstance instance, CancellationToken cancellationToken = default);
    Task<RuntimeEntityInstance?> GetInstanceAsync(string definitionName, string id, CancellationToken cancellationToken = default);
    Task<bool> DeleteInstanceAsync(string definitionName, string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeEntityInstance>> QueryInstancesAsync(string definitionName, string indexedFieldName, object? value, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeEntityAuditRecord>> ListAuditAsync(string subjectId, CancellationToken cancellationToken = default);
}
