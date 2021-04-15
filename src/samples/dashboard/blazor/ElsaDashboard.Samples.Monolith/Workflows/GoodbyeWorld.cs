using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace ElsaDashboard.Samples.Monolith.Workflows
{
    public class GoodbyeWorld : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithDisplayName("Goodbye cruel World!")
                .HttpEndpoint("/goodbye-world")
                .WriteHttpResponse(HttpStatusCode.OK, "Goodbye Cruel World!", "text/plain");
        }
    }
}