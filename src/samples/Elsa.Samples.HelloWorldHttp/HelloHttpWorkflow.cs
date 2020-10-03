using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace Elsa.Samples.HelloWorldHttp
{
    public class HelloHttpWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .ReceiveHttpRequest("/hello")
                .WriteHttpResponse(HttpStatusCode.OK, "Hello World!", "text/html");
        }
    }
}