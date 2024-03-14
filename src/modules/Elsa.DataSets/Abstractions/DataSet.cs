using Elsa.DataSets.Contracts;
using Elsa.DataSets.Models;

namespace Elsa.DataSets.Abstractions;

public abstract class DataSet : IDataSet
{
    public LinkedServiceReference LinkedServiceReference { get; set; }
    public abstract IAsyncEnumerable<T> ReadAsync<T>(ILinkedService linkedService, CancellationToken cancellationToken = default);
}

public abstract class DataSet<TEntity, TLinkedService> : DataSet where TLinkedService : ILinkedService
{
    /// <inheritdoc />
    public override IAsyncEnumerable<T> ReadAsync<T>(ILinkedService linkedService, CancellationToken cancellationToken = default)
    {
        if (linkedService is not TLinkedService service)
            throw new InvalidOperationException($"Expected linked service of type {typeof(TLinkedService).Name} but got {linkedService.GetType().Name}.");

        return (IAsyncEnumerable<T>)ReadAsync(service, cancellationToken);
    }

    /// <summary>
    /// Reads the data from the linked service.
    /// </summary>
    protected abstract IAsyncEnumerable<TEntity> ReadAsync(TLinkedService linkedService, CancellationToken cancellationToken = default);
}