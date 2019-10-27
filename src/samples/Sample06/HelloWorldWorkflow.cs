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
                .StartWith<ReceiveHttpRequest>(
                    activity => activity.Path = new Uri("/hello-world", UriKind.Relative)
                )
                .Then<WriteHttpResponse>(
                    activity =>
                    {
                        activity.Content = new LiteralExpression("<h1>Hello World!</h1><p>Elsa says hi :)</p>");
                        activity.ContentType = "text/html";
                        activity.StatusCode = HttpStatusCode.OK;
                        activity.ResponseHeaders = new LiteralExpression("X-Powered-By=Elsa Workflows");
                    }
                );
        }
    }
}