using System;
using System.Net;
using Elsa.Activities.Http.Activities;
using Elsa.Core.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample06
{
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<HttpRequestTrigger>(activity => activity.Path = new Uri("/hello-world", UriKind.RelativeOrAbsolute))
                .Then<HttpResponseAction>(
                    activity =>
                    {
                        activity.Body = new PlainTextExpression("<h1>Hello World!</h1><p>Elsa says hi :)</p>");
                        activity.ContentType = new PlainTextExpression("text/html");
                        activity.StatusCode = HttpStatusCode.OK;
                        activity.ResponseHeaders = new PlainTextExpression("X-Powered-By=Elsa Workflows");
                    }
                );
        }
    }
}