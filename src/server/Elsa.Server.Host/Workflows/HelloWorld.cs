using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace Elsa.Server.Host.Workflows
{
    public class HelloWorld : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WithDisplayName("Hello World!")
                .HttpRequestReceived("/hello-world")
                .WriteHttpResponse(HttpStatusCode.OK, "Hello World!", "text/plain");
        }
    }
}