using Elsa.Common.Models;
using Elsa.DataSets.Entities;
using Elsa.DataSets.Filters;

namespace Elsa.DataSets.Contracts;

public interface IDataSetDefinitionStore
{
    Task<DataSetDefinition?> FindAsync(string id, CancellationToken cancellationToken = default);
    Task<DataSetDefinition?> FindAsync(DataSetDefinitionFilter filter, CancellationToken cancellationToken = default);
    Task<IEnumerable<DataSetDefinition>> FindManyAsync(DataSetDefinitionFilter filter, CancellationToken cancellationToken = default);
    Task<Page<DataSetDefinition>> FindManyAsync(DataSetDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);
    Task SaveAsync(DataSetDefinition definition, CancellationToken cancellationToken = default);
    Task<long> DeleteAsync(DataSetDefinitionFilter filter, CancellationToken cancellationToken = default);
}