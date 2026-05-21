using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Context;
using System.Text.Json.Nodes;

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
        Assert.Equal("placeholder", context.Data["implementationStatus"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Context resolver redacts sensitive data and metadata keys")]
    public async Task ContextResolverRedactsSensitiveDataAndMetadataKeys()
    {
        var resolver = new AiContextResolver([new SensitiveContextProvider()]);

        var result = await resolver.ResolveAsync(new AiChatRequest
        {
            UserId = "user-1",
            Attachments =
            [
                new AiContextAttachment
                {
                    Kind = "Sensitive"
                }
            ]
        });

        var context = Assert.Single(result);
        Assert.Equal("Handles OAuth token refresh", context.Summary);
        Assert.Equal("[redacted]", context.Data["accessToken"]!.GetValue<string>());
        Assert.Equal("[redacted]", context.Data["description"]!.GetValue<string>());
        Assert.Equal("[redacted]", context.Metadata["apiKey"]!.GetValue<string>());
        Assert.Equal("visible", context.Data["displayName"]!.GetValue<string>());
    }

    [Fact(DisplayName = "Context resolver rejects duplicate provider kinds")]
    public void ContextResolverRejectsDuplicateProviderKinds()
    {
        Assert.Throws<InvalidOperationException>(() => new AiContextResolver([new DuplicateContextProvider("first"), new DuplicateContextProvider("second")]));
    }

    private class SensitiveContextProvider : IAiContextProvider
    {
        public string Kind => "Sensitive";

        public ValueTask<AiResolvedContext> ResolveAsync(AiContextResolutionRequest request, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new AiResolvedContext
            {
                Kind = Kind,
                Summary = "Handles OAuth token refresh",
                Data = new JsonObject
                {
                    ["accessToken"] = "token-value",
                    ["description"] = "Bearer eyJhbGciOi",
                    ["displayName"] = "visible"
                },
                Metadata = new JsonObject
                {
                    ["apiKey"] = "key-value"
                }
            });
        }
    }

    private class DuplicateContextProvider(string summary) : IAiContextProvider
    {
        public string Kind => "Duplicate";

        public ValueTask<AiResolvedContext> ResolveAsync(AiContextResolutionRequest request, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new AiResolvedContext
            {
                Kind = Kind,
                Summary = summary
            });
        }
    }
}
