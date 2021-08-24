using System.Net;
using Elsa.Activities.Http;
using Elsa.Builders;
using Microsoft.AspNetCore.Http;

namespace Elsa.Samples.HttpEndpointSecurity.Workflows
{
    public class SecureHelloWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .HttpEndpoint(setup => setup
                    .WithPath("/safe-hello")
                    .WithMethod("GET")
                    .WithAuthorize()
                    .WithPolicy("IsAdmin"))
                .WriteHttpResponse(setup => setup.WithStatusCode(HttpStatusCode.OK)
                    .WithContent(context =>
                    {
                        var httpContext = context.GetService<IHttpContextAccessor>().HttpContext!;
                        var user = httpContext.User;
                        return $"Hello {user.Identity!.Name}!";
                    }));
        }
    }
}