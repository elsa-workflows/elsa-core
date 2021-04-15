using System;
using System.Net;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Primitives;
using Elsa.Builders;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace ElsaDashboard.Samples.Monolith.Workflows
{
    public class NamingWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithDisplayName("Onboarding")
                .HttpEndpoint("/signup")
                .Correlate(() => Guid.NewGuid().ToString("N"))
                .WriteHttpResponse(x => x.WithStatusCode(HttpStatusCode.OK).WithContent(context => $"Tell me your name please. Use correlation ID {context.WorkflowExecutionContext.CorrelationId}").WithContentType("text/plain"))
                .HttpEndpoint(x => x.WithPath("/signup").WithMethod(HttpMethod.Post.ToString()).WithReadContent())
                .SetVariable("Name", context => (string?) context.GetInput<HttpRequestModel>()!.Body)
                .SetName(context => context.GetVariable<string>("Name"))
                
                .WriteHttpResponse(x => x
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithContent(context => $"Welcome aboard, {context.GetVariable<string>("Name")}!")
                    .WithContentType("text/plain"));
        }
    }
}