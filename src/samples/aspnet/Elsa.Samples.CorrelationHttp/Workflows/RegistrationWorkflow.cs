using System.Net;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Primitives;
using Elsa.Builders;
using Elsa.Samples.CorrelationHttp.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Samples.CorrelationHttp.Workflows
{
    /// <summary>
    /// Triggered when POST HTTP requests are made to /register and continued when a subsequent GET HTTP request is made to /confirm to confirm the registration (see workflows.http).
    /// </summary>
    public class RegistrationWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                // Configure a Receive HTTP Request trigger that executes on incoming HTTP POST requests.
                .HttpEndpoint(activity => activity.WithPath("/register").WithMethod(HttpMethods.Post).WithTargetType<Registration>())
                
                // Store the registration as a workflow variable for easier access.
                .SetVariable(context => (Registration)((HttpRequestModel)(context.Input))?.Body)
                
                // Correlate the workflow by email address.
                .Correlate(context => context.GetVariable<Registration>()!.Email)

                // Write an HTTP response with a hyperlink to continue the workflow (notice the presence of the "correlation" query string parameter). 
                .WriteHttpResponse(activity => activity
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithContentType("text/html")
                    .WithContent(context =>
                    {
                        var registration = context.GetVariable<Registration>()!;
                        return $"Welcome onboard, {registration.Name}! Please <a href=\"http://localhost:8201/confirm?correlation={registration.Email}\">confirm your registration</a>";
                    }))
                
                // Configure another Receive HTTP Request trigger that executes on incoming HTTP GET requests.
                // This will cause the workflow to become suspended and executed once the request comes in.
                .HttpEndpoint(activity => activity.WithPath("/confirm"))
                
                // Write an HTTP response with a thank-you note.
                // Notice that the correct workflow instance is resumed base on the incoming correlation ID.
                .WriteHttpResponse(activity => activity
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithContentType("text/html")
                    .WithContent(
                        context =>
                        {
                            var registration = context.GetVariable<Registration>();
                            return $"Thanks for confirming your registration, {registration.Name}!";    
                        }));
        }
    }
}