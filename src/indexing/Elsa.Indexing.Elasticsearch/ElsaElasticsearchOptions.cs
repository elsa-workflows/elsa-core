using System;

namespace Elsa.Indexing
{
    public class ElsaElasticsearchOptions
    {
        public Uri[] Uri { get; set; }

        public string WorkflowDefinitionIndexName { get; set; } = "elsa_workflow_definition";

        public string WorkflowInstanceIndexName { get; set; } = "elsa_workflow_instance";
    }
}
