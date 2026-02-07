using Elsa.Workflows;

namespace Elsa.Http.IntegrationTests.Activities.Workflows;

/// <summary>
/// A simple workflow that attempts to write an HTTP response without HTTP context.
/// </summary>
internal class WriteHttpResponseWithoutHttpContextWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new WriteHttpResponse
        {
            Content = new("This should fail"),
            StatusCode = new(System.Net.HttpStatusCode.OK)
        };
    }
}

