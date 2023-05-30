using JetBrains.Annotations;

namespace Elsa.MongoDB.Options;

/// <summary>
/// Contains MongoDb settings.
/// </summary>
[PublicAPI]
public class MongoDbOptions
{
    /// <summary>
    /// The connection string that contains username and password to connect to the MongoDb client. 
    /// </summary>
    public string ConnectionString { get; set; } = default!;
    
    /// <summary>
    /// The database name to use when connecting to the MongoDb client.
    /// </summary>
    public string? DatabaseName { get; set; }
}