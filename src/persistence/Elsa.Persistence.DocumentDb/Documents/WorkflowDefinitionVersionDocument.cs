using Elsa.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Elsa.Persistence.DocumentDb.Documents
{
    public class WorkflowDefinitionVersionDocument : DocumentBase
    {
        internal const string COLLECTION_NAME = "WorkflowDefinition";

        [JsonProperty(PropertyName = "type")] 
        public string Type { get; } = nameof(WorkflowDefinitionVersionDocument);

        [JsonProperty(PropertyName = "definitionId")]
        public string DefinitionId { get; set; }

        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        [JsonProperty(PropertyName = "name")] 
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "activities")]
        public IList<ActivityDefinition> Activities { get; set; }

        [JsonProperty(PropertyName = "connections")]
        public IList<ConnectionDefinition> Connections { get; set; }

        [JsonProperty(PropertyName = "variables")]
        public Variables Variables { get; set; }

        [JsonProperty(PropertyName = "isSingleton")]
        public bool IsSingleton { get; set; }

        [JsonProperty(PropertyName = "isDisabled")]
        public bool IsDisabled { get; set; }

        [JsonProperty(PropertyName = "isPublished")]
        public bool IsPublished { get; set; }

        [JsonProperty(PropertyName = "isLatest")]
        public bool IsLatest { get; set; }
    }
}