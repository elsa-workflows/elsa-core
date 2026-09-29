namespace Elsa.Sql.Contracts;

public interface ISqlClientNamesProvider
{
    /// <summary>
    /// Returns a dictionary of registered clients.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor cancellation requests.</param>
    /// <returns>A <see cref="Dictionary{TKey, TValue}"/> of registered client names their <see cref="Type"/>.</returns>
    Task<IReadOnlyDictionary<string, Type>> GetRegisteredSqlClientNamesAsync(CancellationToken cancellationToken);
}