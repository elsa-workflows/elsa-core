using Newtonsoft.Json;

namespace Elsa.Persistence.DocumentDb.Documents
{
    public abstract class DocumentBase
    {
        [JsonProperty(PropertyName = "id")] public string Id { get; set; }
        [JsonProperty(PropertyName = "_rid")] public string ResourceId { get; set; }
        [JsonProperty(PropertyName = "_etag")] public string ETag { get; set; }
        [JsonProperty(PropertyName = "_self")] public string SelfLink { get; set; }
    }
}