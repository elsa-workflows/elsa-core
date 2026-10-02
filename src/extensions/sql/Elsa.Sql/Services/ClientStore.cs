using Elsa.Sql.Client;

namespace Elsa.Sql.Services;
public class ClientStore
{
    private readonly Dictionary<string, Type> clients = new();

    /// <summary>
    /// Dictionary of registered clients and their type.
    /// </summary>
    public IReadOnlyDictionary<string, Type> Clients => clients;

    /// <summary>
    /// Registers the specified client type <typeparamref name="TClient"/> with the store.
    /// The client type must inherit from <see cref="ISqlClient"/>.
    /// </summary>
    /// <typeparam name="TClient">
    /// The type of the client to be registered. The client must be a class that implements the <see cref="ISqlClient"/> interface.
    /// </typeparam>
    /// <param name="name">
    /// The name of the client to register. If not provided, the name defaults to <c>nameof(TClient)</c>. 
    /// This value is used as a key to identify the client in the store.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a client with the same name is already registered in the store.
    /// </exception>
    /// <remarks>
    /// This method registers a client type to the store using a unique key. The key is either the provided <paramref name="name"/> or the default name derived from <typeparamref name="TClient"/>.
    /// </remarks>
    public void Register<TClient>(string? name) where TClient : class, ISqlClient
    {
        var key = string.IsNullOrEmpty(name) ? nameof(TClient) : name;
        if (clients.ContainsKey(key)) { throw new InvalidOperationException($"Client with key '{name}' is already registered."); }
        clients.Add(key, typeof(TClient));
    }
}