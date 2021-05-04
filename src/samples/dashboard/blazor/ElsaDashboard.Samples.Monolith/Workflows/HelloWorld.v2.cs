using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace ElsaDashboard.Samples.Monolith.Workflows
{
    public class HelloWorldV2 : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithWorkflowDefinitionId("HelloWorld")
                .WithVersion(2)
                .WithDisplayName("Hello World!")
                .HttpEndpoint("/hello-world/v2")
                .WriteHttpResponse(HttpStatusCode.OK, "Hello World V2!", "text/plain");
        }
    }
}