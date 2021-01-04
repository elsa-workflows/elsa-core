using Elsa.Persistence.DocumentDb.Extensions;
using Newtonsoft.Json;

namespace Elsa.Persistence.DocumentDb.Documents
{
    public abstract class DocumentBase
    {
        [JsonProperty(PropertyName = "id")] 
        public string Id { get; set; }
        
        [JsonProperty(PropertyName = "_rid")] 
        public string ResourceId { get; set; }
        
        [JsonProperty(PropertyName = "_etag")] 
        public string ETag { get; set; }

        [JsonProperty(PropertyName = "_self")] 
        public string SelfLink { get; set; }

        [JsonProperty(PropertyName = "tenantId")]
        public string TenantId { get; set; }

        public static K GetCollectionName<T,K>() where T : DocumentBase where K : class => 
            typeof(T).GetConstantValue<K>("COLLECTION_NAME");
    }
}