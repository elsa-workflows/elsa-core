using System.Security.Authentication;
using JetBrains.Annotations;
using MongoDB.Driver;

namespace Elsa.MongoDb.Options;

/// <summary>
/// Contains MongoDb settings.
/// </summary>
[PublicAPI]
public class MongoDbOptions
{
    /// <summary>
    /// The database name to use when connecting to the MongoDb client.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// The write concern to use when writing to a MongoDb collection.
    /// </summary>
    public WriteConcern WriteConcern { get; set; } = WriteConcern.WMajority;

    /// <summary>
    /// The read concern to use when reading from a MongoDb collection.
    /// </summary>
    public ReadConcern ReadConcern { get; set; } = ReadConcern.Available;
    
    /// <summary>
    /// The read preference to use when reading from a MongoDb collection.
    /// </summary>
    public ReadPreference ReadPreference { get; set; } = ReadPreference.Nearest;

    /// <summary>
    /// Whether to retry reads.
    /// </summary>
    public bool RetryReads { get; set; } = true;

    /// <summary>
    /// Whether to retry writes.
    /// </summary>
    public bool RetryWrites { get; set; } = true;

    /// <summary>
    /// The SSL settings to use when connecting to the MongoDb client.
    /// </summary>
    public SslSettings SslSettings { get; set; } = new() { EnabledSslProtocols = SslProtocols.Tls12 };
}