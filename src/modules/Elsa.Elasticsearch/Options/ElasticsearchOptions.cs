using Humanizer;
using JetBrains.Annotations;

namespace Elsa.Elasticsearch.Options;

/// <summary>
/// Contains Elasticsearch settings.
/// </summary>
[PublicAPI]
public class ElasticsearchOptions
{
    /// <summary>
    /// The URL of the Elasticsearch server or the name of a connection string that in turn stores the URL. 
    /// </summary>
    public string Endpoint { get; set; } = default!;
    
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
    /// A map between type and index name to use. When no index name is configured for a given type, the name of the type is used.
    /// </summary>
    public IDictionary<Type, string> IndexNameMappings { get; set; } = new Dictionary<Type, string>();
    
    /// <summary>
    /// Returns an index name for the specified document type. If no mapping was found, the simple name of the type is used.
    /// </summary>
    public string GetIndexNameFor<T>() => GetIndexNameFor(typeof(T));
    
    /// <summary>
    /// Returns an index name for the specified document type. If no mapping was found, the simple name of the type is used.
    /// </summary>
    public string GetIndexNameFor(Type documentType) => IndexNameMappings.TryGetValue(documentType, out var index) ? index : documentType.Name.Dasherize();
}