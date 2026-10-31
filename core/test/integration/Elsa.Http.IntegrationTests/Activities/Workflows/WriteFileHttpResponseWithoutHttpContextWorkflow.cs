using Elsa.Workflows;

namespace Elsa.Http.IntegrationTests.Activities.Workflows;

/// <summary>
/// A workflow that attempts to write a file HTTP response without HTTP context.
/// </summary>
internal class WriteFileHttpResponseWithoutHttpContextWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new WriteFileHttpResponse
        {
            Content = new(new byte[] { 1, 2, 3 }),
            Filename = new("test.bin")
        };
    }
}

