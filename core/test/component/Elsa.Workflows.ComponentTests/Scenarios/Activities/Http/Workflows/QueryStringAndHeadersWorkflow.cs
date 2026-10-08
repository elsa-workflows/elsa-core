using System.Net;
using Elsa.Http;
using Elsa.Workflows.Activities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http.Workflows;

public class QueryStringAndHeadersWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        
        var queryDataVariable = builder.WithVariable<IDictionary<string, object>>();
        var headersVariable = builder.WithVariable<IDictionary<string, object>>();
        
        builder.Root = new Sequence
        {
            Activities =
            [
                new HttpEndpoint
                {
                    Path = new("test/query-headers"),
                    SupportedMethods = new([HttpMethods.Get]),
                    CanStartWorkflow = true,
                    QueryStringData = new(queryDataVariable),
                    Headers = new(headersVariable)
                },
                new WriteHttpResponse
                {
                    Content = new(context => 
                    {
                        var queryData = queryDataVariable.Get(context);
                        var headers = headersVariable.Get(context);
                        
                        var nameParam = queryData?.ContainsKey("name") == true ? queryData["name"]?.ToString() : "unknown";
                        var userAgent = headers?.ContainsKey("User-Agent") == true ? headers["User-Agent"]?.ToString() : "unknown";
                        
                        return $"Name: {nameParam}, UserAgent: {userAgent}";
                    }),
                    ContentType = new("text/plain"),
                    StatusCode = new(HttpStatusCode.OK)
                }
            ]
        };
    }
}

