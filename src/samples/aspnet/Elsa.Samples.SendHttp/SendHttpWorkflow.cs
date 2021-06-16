using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using System;
using System.Text.Json;

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
                    .WithContent(async context => JsonSerializer.Serialize(await context.GetNamedActivityPropertyAsync<SendHttpRequest, object>("TestHttpRequest", x => x.ResponseContent))));
        }
    }
}
