using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace Elsa.Samples.FaultyWorkflows
{
    /// <summary>
    /// A workflow that is triggered when HTTP requests are made to /hello and writes a response.
    /// </summary>
    public class FaultyWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .HttpRequestReceived("/hello")
                .WriteHttpResponse(HttpStatusCode.OK, "Hello World!", "text/html");
        }
    }
}