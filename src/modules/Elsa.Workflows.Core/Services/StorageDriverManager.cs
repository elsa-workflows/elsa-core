using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class StorageDriverManager : IStorageDriverManager
{
    private readonly IEnumerable<IStorageDriver> _drivers;
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public StorageDriverManager(IEnumerable<IStorageDriver> drivers) => _drivers = drivers;

    /// <inheritdoc />
    public IStorageDriver? Get(Type type) => _drivers.FirstOrDefault(x => x.GetType() == type);

    /// <inheritdoc />
    public IEnumerable<IStorageDriver> List() => _drivers;
}