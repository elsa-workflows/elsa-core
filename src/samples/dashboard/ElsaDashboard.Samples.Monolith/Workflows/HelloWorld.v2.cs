using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace Elsa.Samples.Server.Host.Workflows
{
    public class HelloWorldV2 : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WithWorkflowDefinitionId("HelloWorld")
                .WithVersion(2)
                .WithDisplayName("Hello World!")
                .HttpRequestReceived("/hello-world/v2")
                .WriteHttpResponse(HttpStatusCode.OK, "Hello World V2!", "text/plain");
        }
    }
}