using System.Security.Claims;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Endpoints.Ai.Tools;
using Elsa.AI.Host.Extensions;
using Elsa.AI.Host.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using Request = Elsa.AI.Host.Endpoints.Ai.Tools.Request;
using ToolsEndpoint = Elsa.AI.Host.Endpoints.Ai.Tools.Endpoint;

namespace Elsa.AI.IntegrationTests;

public class AiToolsEndpointTests
{
    [Fact(DisplayName = "Tools endpoint returns enabled registry results")]
    public async Task ToolsEndpointReturnsEnabledRegistryResults()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        using var provider = services.BuildServiceProvider();
        var endpoint = new ToolsEndpoint(provider.GetRequiredService<IAiToolRegistry>(), MicrosoftOptions.Create(new AiHostOptions()));

        var tools = await endpoint.ExecuteAsync(new Request(), CancellationToken.None);

        Assert.Empty(tools);
    }

    [Fact(DisplayName = "Tools endpoint forwards agent scope to registry")]
    public async Task ToolsEndpointForwardsAgentScopeToRegistry()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddSingleton<IAiTool, WorkflowAuthorTool>();
        services.AddSingleton<IAiTool, WorkflowEditorTool>();
        using var provider = services.BuildServiceProvider();
        var endpoint = new ToolsEndpoint(provider.GetRequiredService<IAiToolRegistry>(), MicrosoftOptions.Create(new AiHostOptions()));
        SetHttpContext(endpoint, "workflows:author");

        var tools = await endpoint.ExecuteAsync(new Request { Agent = "workflow-author" }, CancellationToken.None);

        var tool = Assert.Single(tools);
        Assert.Equal("workflow.author", tool.Name);
    }

    private class WorkflowAuthorTool : IAiTool
    {
        public AiToolDefinition Definition { get; } = new()
        {
            Name = "workflow.author",
            DisplayName = "Workflow author",
            AgentScopes = ["workflow-author"],
            Permissions = ["workflows:author"]
        };

        public ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiToolResult());
    }

    private class WorkflowEditorTool : IAiTool
    {
        public AiToolDefinition Definition { get; } = new()
        {
            Name = "workflow.editor",
            DisplayName = "Workflow editor",
            AgentScopes = ["workflow-editor"],
            Permissions = ["workflows:editor"]
        };

        public ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiToolResult());
    }

    private static void SetHttpContext(ToolsEndpoint endpoint, params string[] permissions)
    {
        var identity = new ClaimsIdentity(permissions.Select(x => new Claim(PermissionNames.ClaimType, x)), "test");
        var property = typeof(ToolsEndpoint)
            .GetProperty(nameof(ToolsEndpoint.HttpContext))!;
        property.SetValue(endpoint, new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        });
    }
}
