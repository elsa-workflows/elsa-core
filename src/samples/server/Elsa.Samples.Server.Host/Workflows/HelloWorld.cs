using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace Elsa.Samples.Server.Host.Workflows
{
    public class HelloWorld : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithWorkflowDefinitionId("HelloWorld")
                .WithVersion(1, false, false)
                .WithDisplayName("Hello World!")
                .HttpEndpoint("/hello-world")
                .WriteHttpResponse(HttpStatusCode.OK, "Hello World!", "text/plain");
        }
    }
}