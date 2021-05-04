using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace Elsa.Samples.InvokeWorkflowFromController.Workflows
{
    /// <summary>
    /// A workflow that takes us to the moon.
    /// </summary>
    public class RocketWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteHttpResponse(activity => activity
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithContentType("text/plain")
                    .WithContent("We're going to the moon!"));
        }
    }
}