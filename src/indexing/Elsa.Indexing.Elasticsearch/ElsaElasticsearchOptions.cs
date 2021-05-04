using System;

namespace Elsa.Indexing
{
    public class ElsaElasticsearchOptions
    {
        public Uri[] Uri { get; set; } = new Uri[0];

        public string WorkflowDefinitionIndexName { get; set; } = "elsa_workflow_definition";

        public string WorkflowInstanceIndexName { get; set; } = "elsa_workflow_instance";

        public BasicAuthenticationCredentials? BasicAuthentication { get; set; }

        public ApiKeyAuthenticationCredentials? ApiKeyAuthentication { get; set; }

        public class BasicAuthenticationCredentials
        {
            public string Username { get; set; } = default!;
            public string Password { get; set; } = default!;
        }

        public class ApiKeyAuthenticationCredentials
        {
            public string Id { get; set; } = default!;
            public string ApiKey { get; set; } = default!;
        }
    }
}
