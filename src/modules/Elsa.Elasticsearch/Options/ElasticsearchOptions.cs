namespace Elsa.Elasticsearch.Options;

public class ElasticsearchOptions
{
    public const string Elasticsearch = "Elasticsearch";

    public Dictionary<string, string>? Aliases { get; set; }
    public string Endpoint { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string ApiKey { get; set; }
}