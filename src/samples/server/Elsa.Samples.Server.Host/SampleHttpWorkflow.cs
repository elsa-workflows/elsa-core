using System;
using System.Net.Http;
using Elsa.Activities.Console;
using Elsa.Activities.Http;
using Elsa.Builders;

namespace Elsa.Samples.Server.Host;

public class SampleHttpWorkflow : IWorkflow
{
    public void Build(IWorkflowBuilder builder)
    {
        builder.SendHttpRequest(activity => activity
            .WithUrl(new Uri("https://reqres.in/api/login"))
            .WithMethod(HttpMethod.Post.Method)
            .WithContent("{\"email\": \"peter@klaven\"}")
            .WithReadContent(true)
            .WithSupportedHttpCodes(new[]{ 200, 400 }),
            activity =>
            {
                activity.When("200").WriteLine("OK!");
                activity.When("400").WriteLine("Missing some fields!");
                activity.When("500").WriteLine("Server is sad :(");
            });
    }
}