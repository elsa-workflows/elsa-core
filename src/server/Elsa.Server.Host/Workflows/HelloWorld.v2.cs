using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace Elsa.Server.Host.Workflows
{
    public class HelloWorldV2 : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WithVersion(2)
                .WithDisplayName("Hello World!")
                .HttpRequestReceived("/hello-world")
                .WriteHttpResponse(HttpStatusCode.OK, "Hello World V2!", "text/plain");
        }
    }
}