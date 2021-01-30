using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace Elsa.Samples.Server.Host.Workflows
{
    public class GoodbyeWorld : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithDisplayName("Goodbye cruel World!")
                .HttpRequestReceived("/goodbye-world")
                .WriteHttpResponse(HttpStatusCode.OK, "Goodbye Cruel World!", "text/plain");
        }
    }
}