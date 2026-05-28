using System.Security.Claims;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Endpoints.AI.Tools;
using Elsa.AI.Host.Options;
using Elsa.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using Request = Elsa.AI.Host.Endpoints.AI.Tools.Request;
using ToolsEndpoint = Elsa.AI.Host.Endpoints.AI.Tools.Endpoint;

namespace Elsa.AI.IntegrationTests;

public class AIToolsEndpointTests
{
    [Fact(DisplayName = "Tools endpoint returns enabled registry results")]
    public async Task ToolsEndpointReturnsEnabledRegistryResults()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        using var provider = services.BuildServiceProvider();
        var endpoint = new ToolsEndpoint(provider.GetRequiredService<IAIToolRegistry>(), MicrosoftOptions.Create(new AIHostOptions()));

        var tools = await endpoint.ExecuteAsync(new Request(), CancellationToken.None);

        Assert.Empty(tools);
    }

    [Fact(DisplayName = "Tools endpoint forwards agent scope to registry")]
    public async Task ToolsEndpointForwardsAgentScopeToRegistry()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddSingleton<IAITool, WorkflowAuthorTool>();
        services.AddSingleton<IAITool, WorkflowEditorTool>();
        using var provider = services.BuildServiceProvider();
        var endpoint = new ToolsEndpoint(provider.GetRequiredService<IAIToolRegistry>(), MicrosoftOptions.Create(new AIHostOptions()));
        SetHttpContext(endpoint, "workflows:author");

        var tools = await endpoint.ExecuteAsync(new Request { Agent = "workflow-author" }, CancellationToken.None);

        var tool = Assert.Single(tools);
        Assert.Equal("workflow.author", tool.Name);
    }

    [Fact(DisplayName = "Tool registry caches definitions across list calls")]
    public async Task ToolRegistryCachesDefinitionsAcrossListCalls()
    {
        CountingTool.Reset();
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddTransient<IAITool>(_ => CountingTool.Create());
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IAIToolRegistry>();

        await registry.ListAsync(new AIToolQuery(), CancellationToken.None);
        await registry.ListAsync(new AIToolQuery(), CancellationToken.None);

        Assert.Equal(1, CountingTool.ConstructorCount);
    }

    private class WorkflowAuthorTool : IAITool
    {
        public AIToolDefinition Definition { get; } = new()
        {
            Name = "workflow.author",
            DisplayName = "Workflow author",
            AgentScopes = ["workflow-author"],
            Permissions = ["workflows:author"]
        };

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIToolResult());
    }

    private class WorkflowEditorTool : IAITool
    {
        public AIToolDefinition Definition { get; } = new()
        {
            Name = "workflow.editor",
            DisplayName = "Workflow editor",
            AgentScopes = ["workflow-editor"],
            Permissions = ["workflows:editor"]
        };

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIToolResult());
    }

    private class CountingTool : IAITool
    {
        private static int _constructorCount;

        public static int ConstructorCount => _constructorCount;

        private CountingTool()
        {
        }

        public static CountingTool Create()
        {
            Interlocked.Increment(ref _constructorCount);
            return new CountingTool();
        }

        public AIToolDefinition Definition { get; } = new()
        {
            Name = "counting.tool",
            DisplayName = "Counting tool"
        };

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIToolResult());

        public static void Reset()
        {
            Interlocked.Exchange(ref _constructorCount, 0);
        }
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
