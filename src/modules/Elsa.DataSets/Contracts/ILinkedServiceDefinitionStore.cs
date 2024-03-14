using Elsa.Common.Models;
using Elsa.DataSets.Entities;
using Elsa.DataSets.Filters;

namespace Elsa.DataSets.Contracts;

public interface ILinkedServiceDefinitionStore
{
    Task<LinkedServiceDefinition?> FindAsync(string id, CancellationToken cancellationToken = default);
    Task<LinkedServiceDefinition?> FindAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default);
    Task<IEnumerable<LinkedServiceDefinition>> FindManyAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default);
    Task<Page<LinkedServiceDefinition>> FindManyAsync(LinkedServiceDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);
    Task SaveAsync(LinkedServiceDefinition definition, CancellationToken cancellationToken = default);
    Task<long> DeleteAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default);
}