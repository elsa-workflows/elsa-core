using System.Net;
using Elsa.Http;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http.Workflows;

public class SecurityTestWorkflow : WorkflowBase
{
    private static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        
        builder.Root = new Sequence
        {
            Activities =
            [
                new HttpEndpoint
                {
                    Path = new("test/secure"),
                    SupportedMethods = new([HttpMethods.Get, HttpMethods.Post]),
                    CanStartWorkflow = true
                },
                // Hardcoded response that definitely returns Unauthorized (401)
                new WriteHttpResponse
                {
                    Content = new("SECURITY_TEST_UNAUTHORIZED"),
                    ContentType = new("text/plain"),
                    StatusCode = new(HttpStatusCode.Unauthorized)
                }
            ]
        };
    }
}

public class BlockedFileExtensionWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        
        var filesVariable = builder.WithVariable<IFormFile[]>();
        
        var httpEndpoint = new HttpEndpoint
        {
            Path = new("test/blocked-extensions"),
            SupportedMethods = new([HttpMethods.Post]),
            CanStartWorkflow = true,
            Files = new(filesVariable),
            BlockedFileExtensions = new([".exe", ".bat", ".sh"]),
            ExposeInvalidFileExtensionOutcome = true
        };
        
        var successResponse = new WriteHttpResponse
        {
            Content = new("File upload successful"),
            ContentType = new("text/plain"),
            StatusCode = new(HttpStatusCode.OK)
        };
        
        var errorResponse = new WriteHttpResponse
        {
            Content = new("Blocked file extension detected"),
            ContentType = new("text/plain"),
            StatusCode = new(HttpStatusCode.UnsupportedMediaType)
        };
        
        builder.Root = new Flowchart
        {
            Start = httpEndpoint,
            Activities = { httpEndpoint, successResponse, errorResponse },
            Connections = 
            {
                new Connection(new Elsa.Workflows.Activities.Flowchart.Models.Endpoint(httpEndpoint, "Done"), new Elsa.Workflows.Activities.Flowchart.Models.Endpoint(successResponse)),
                new Connection(new Elsa.Workflows.Activities.Flowchart.Models.Endpoint(httpEndpoint, "Invalid file extension"), new Elsa.Workflows.Activities.Flowchart.Models.Endpoint(errorResponse))
            }
        };
    }
}


