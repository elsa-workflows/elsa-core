using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Elsa.Samples.SendHttp
{
    public class SendHttpWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder.WithWorkflowDefinitionId("SendHttp")
                .WithVersion(1)
                .WithDescription("Send test http request and return response")
                .HttpEndpoint(setup => setup.WithPath("/helloworld")
                    .WithMethod("GET"))
                .SendHttpRequest(setup => setup.WithUrl(new Uri("https://jsonplaceholder.typicode.com/todos/3"))
                    .WithMethod("GET")
                    .WithReadContent(true))
                .WithName("TestHttpRequest")
                .WriteHttpResponse(setup => setup.WithStatusCode(System.Net.HttpStatusCode.OK)
                    .WithContent(x => JsonSerializer.Serialize(x.GetOutputFrom<HttpResponseModel>("TestHttpRequest").Content)));
        }
    }
}
