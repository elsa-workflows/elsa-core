namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Provides access to registered <see cref="IStorageDriver"/> objects.
/// </summary>
public interface IStorageDriverManager
{
    /// <summary>
    /// Returns a <see cref="IStorageDriver"/> by type.
    /// </summary>
    IStorageDriver? Get(Type type);
    
    /// <summary>
    /// Return a list of all registered <see cref="IStorageDriver"/> implementations.
    /// </summary>
    IEnumerable<IStorageDriver> List();
}