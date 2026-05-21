using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Context;

namespace Elsa.AI.Host.UnitTests.Context;

public class AiContextResolverTests
{
    [Fact(DisplayName = "Context resolver resolves workflow definition references server-side")]
    public async Task ContextResolverResolvesWorkflowDefinitionReferences()
    {
        var resolver = new AiContextResolver([new WorkflowDefinitionContextProvider()]);

        var result = await resolver.ResolveAsync(new AiChatRequest
        {
            UserId = "user-1",
            Attachments =
            [
                new AiContextAttachment
                {
                    Kind = "WorkflowDefinition",
                    ReferenceId = "workflow-1"
                }
            ]
        });

        var context = Assert.Single(result);
        Assert.Equal("WorkflowDefinition", context.Kind);
        Assert.Equal("workflow-1", context.ReferenceId);
    }
}
