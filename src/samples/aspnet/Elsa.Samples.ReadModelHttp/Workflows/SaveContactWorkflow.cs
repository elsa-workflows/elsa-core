using System.Net;
using Elsa.Activities.Console;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Builders;
using Elsa.Samples.ReadModelHttp.Models;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Elsa.Samples.ReadModelHttp.Workflows
{
    /// <summary>
    /// A workflow that is triggered when POST HTTP requests are made to /contacts (see workflows.http).
    /// </summary>
    public class SaveContactWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            var serializer = builder.ServiceProvider.GetRequiredService<IContentSerializer>();

            builder
                // Configure a Receive HTTP Request trigger that executes on incoming HTTP POST requests.
                .HttpEndpoint(activity => activity.WithPath("/contacts").WithMethod(HttpMethods.Post).WithTargetType<Contact>()).WithName("HttpRequest")

                // Write something to the console to demonstrate that we can receive the outcome of the previous activity (HttpRequest) as an input to this WriteLine activity. 
                .WriteLine(context => $"Received some contact details: {JsonConvert.SerializeObject(context.Input)}")

                // Write an HTTP response to demonstrate that we can access any activity's outcome using the activity name.
                .WriteHttpResponse(activity => activity
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithContentType("application/json")
                    .WithContent(async context =>
                    {
                        var request = await context.GetNamedActivityPropertyAsync<HttpEndpoint, HttpRequestModel>("HttpRequest", x => x.Output);
                        var contact = request!.GetBody<Contact>();
                        return serializer.Serialize(contact);
                    }));
        }
    }
}