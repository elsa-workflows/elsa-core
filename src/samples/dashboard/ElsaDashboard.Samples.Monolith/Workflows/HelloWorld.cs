using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace ElsaDashboard.Samples.Monolith.Workflows
{
    public class HelloWorld : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WithWorkflowDefinitionId("HelloWorld")
                .WithVersion(1)
                .WithDisplayName("Hello World!")
                .HttpRequestReceived("/hello-world")
                .WriteHttpResponse(HttpStatusCode.OK, "Hello World!", "text/plain");
        }
    }
}