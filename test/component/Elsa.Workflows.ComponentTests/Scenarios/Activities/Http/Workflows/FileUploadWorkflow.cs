using System.Net;
using Elsa.Http;
using Elsa.Workflows.Activities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Http.Workflows;

public class FileUploadWorkflow : WorkflowBase
{
    private static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        
        var filesVariable = builder.WithVariable<IFormFile[]>();
        var fileVariable = builder.WithVariable<IFormFile>();
        
        builder.Root = new Sequence
        {
            Activities =
            [
                new HttpEndpoint
                {
                    Path = new("test/file-upload"),
                    SupportedMethods = new([HttpMethods.Post]),
                    CanStartWorkflow = true,
                    Files = new(filesVariable),
                    File = new(fileVariable)
                },
                new WriteHttpResponse
                {
                    Content = new(context => 
                    {
                        var files = filesVariable.Get(context);
                        var firstFile = fileVariable.Get(context);
                        
                        if (files?.Length > 0)
                        {
                            var fileInfos = files.Select(f => $"{f.FileName} ({f.Length} bytes, {f.ContentType})");
                            return $"Files uploaded: {string.Join(", ", fileInfos)}";
                        }
                        
                        return "No files uploaded";
                    }),
                    ContentType = new("text/plain"),
                    StatusCode = new(HttpStatusCode.OK)
                }
            ]
        };
    }
}

