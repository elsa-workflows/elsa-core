using System.Linq;
using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Samples.CustomActivities
{
    public class EchoQueryStringWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .HttpEndpoint("/test")
                .Then<ReadQueryString>()
                .WriteHttpResponse(_ => HttpStatusCode.OK, GenerateSomeHtml, _ => "text/html");
        }

        private string GenerateSomeHtml(ActivityExecutionContext context)
        {
            var query = (IQueryCollection)context.Input!; // The output of the ReadQueryString activity will be available as input to this one.
            var items = query!.Select(x => $"<li>{x.Key}: {x.Value}</li>");

            return $"<ul>{string.Join("\n", items)}</ul>";
        }
    }
}