using Elsa.DataSets.Models;

namespace Elsa.DataSets.Contracts;

public interface IDataSet
{
    IAsyncEnumerable<T> ReadAsync<T>(ILinkedService linkedService, CancellationToken cancellationToken = default);
}