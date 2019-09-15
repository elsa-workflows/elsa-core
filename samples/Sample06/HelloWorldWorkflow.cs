using System;
using System.Net;
using Elsa.Activities.Http.Activities;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample06
{
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<HttpRequestEvent>(activity => activity.Path = new Uri("/hello-world", UriKind.RelativeOrAbsolute))
                .Then<HttpResponseAction>(
                    activity =>
                    {
                        activity.Content = new PlainTextExpression("<h1>Hello World!</h1><p>Elsa says hi :)</p>");
                        activity.ContentType = "text/html";
                        activity.StatusCode = HttpStatusCode.OK;
                        activity.ResponseHeaders = new PlainTextExpression("X-Powered-By=Elsa Workflows");
                    }
                );
        }
    }
}