using Newtonsoft.Json;
using System;

namespace Elsa.Persistence.DocumentDb.Documents
{
    public class WorkflowDefinitionDocument
    {
        [JsonProperty(PropertyName = "id")] public string Id { get; set; }

        [JsonProperty(PropertyName = "tenantId")]
        public string TenantId { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
