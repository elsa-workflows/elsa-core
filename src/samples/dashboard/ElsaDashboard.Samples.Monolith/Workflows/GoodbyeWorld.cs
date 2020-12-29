using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace ElsaDashboard.Samples.Monolith.Workflows
{
    public class GoodbyeWorld : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WithDisplayName("Goodbye cruel World!")
                .HttpRequestReceived("/goodbye-world")
                .WriteHttpResponse(HttpStatusCode.OK, "Goodbye Cruel World!", "text/plain");
        }
    }
}