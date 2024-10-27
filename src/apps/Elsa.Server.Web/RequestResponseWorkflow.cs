using System.Net.Mime;
using Elsa.Http;
using Elsa.Samples.AspNet.HttpEndpoints.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Samples.AspNet.HttpEndpoints.Workflows;

public class RequestResponseWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var product = builder.WithVariable<Product>();
        var routeData = builder.WithVariable<IDictionary<string, object>>();
        var productId = builder.WithVariable<int>();

        builder.Root = new Sequence
        {
            Activities =
            {
                new HttpEndpoint
                {
                    Path = new("api/products/{id}"),
                    SupportedMethods = new([HttpMethods.Get]),
                    RouteData = new(routeData),
                    CanStartWorkflow = true
                },
                new SetVariable
                {
                    Variable = productId,
                    Value = new(context => routeData.Get(context)!["id"])
                },
                new SendHttpRequest
                {
                    Url = new(context =>
                    {
                        var id = productId.Get(context);
                        return new Uri($"https://fakestoreapi.com/products/{id}");
                    }),
                    Method = new(HttpMethods.Get),
                    ParsedContent = new(product)
                },
                new WriteHttpResponse
                {
                    ContentType = new(MediaTypeNames.Application.Json),
                    Content = new(context => product.Get(context))
                }
            }
        };
    }
}