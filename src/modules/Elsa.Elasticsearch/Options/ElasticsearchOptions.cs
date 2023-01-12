namespace Elsa.Elasticsearch.Options;

/// <summary>
/// Contains Elasticsearch settings.
/// </summary>
public class ElasticsearchOptions
{
    /// <summary>
    /// The URL of the Elasticsearch server. 
    /// </summary>
    public Uri Endpoint { get; set; } = default!;
    
    /// <summary>
    /// The username to use when connecting with the Elasticsearch server.
    /// </summary>
    public string? Username { get; set; }
    
    /// <summary>
    /// The password to use when connecting with the Elasticsearch server.
    /// </summary>
    public string? Password { get; set; }
    
    /// <summary>
    /// The API key to use when connecting with the Elasticsearch server.
    /// </summary>
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// The interval to attempt a rollover.
    /// </summary>
    public TimeSpan RolloverInterval { get; set; } = TimeSpan.FromDays(10);

    /// <summary>
    /// A map between type and index name to use. When no index name is configured for a given type, the name of the type is used.
    /// </summary>
    public IDictionary<Type, string> IndexNameMappings { get; set; } = new Dictionary<Type, string>();
}