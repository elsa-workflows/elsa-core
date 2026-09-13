using System.Reflection;
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
    [Test]
    [DisplayName("Tools endpoint returns enabled registry results")]
    public async Task ToolsEndpointReturnsEnabledRegistryResults()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        using var provider = services.BuildServiceProvider();
        var endpoint = new ToolsEndpoint(provider.GetRequiredService<IAIToolRegistry>(), MicrosoftOptions.Create(new AIHostOptions()));

        var tools = await endpoint.ExecuteAsync(new Request(), CancellationToken.None);

        await Assert.That(tools).Contains(x => x.Name == "activities.search");
        await Assert.That(tools).Contains(x => x.Name == "workflows.search");
        await Assert.That(tools).Contains(x => x.Name == "instances.search");
    }

    [Test]
    [DisplayName("Tools endpoint forwards agent scope to registry")]
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

        await Assert.That(tools).Contains(tool => tool.Name == "workflow.author");
        await Assert.That(tools).DoesNotContain(tool => tool.Name == "workflow.editor");
    }

    [Test]
    [DisplayName("Tool registry caches definitions across list calls")]
    public async Task ToolRegistryCachesDefinitionsAcrossListCalls()
    {
        var constructionCounter = new ConstructionCounter();
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddTransient<IAITool>(_ => new CountingTool(constructionCounter));
        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IAIToolRegistry>();

        await registry.ListAsync(new AIToolQuery(), CancellationToken.None);
        await registry.ListAsync(new AIToolQuery(), CancellationToken.None);

        await Assert.That(constructionCounter.Count).IsEqualTo(1);
    }

    [Test]
    [DisplayName("Tools endpoint lists built-in grounding tools")]
    public async Task ToolsEndpointListsBuiltInGroundingTools()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        using var provider = services.BuildServiceProvider();
        var endpoint = new ToolsEndpoint(provider.GetRequiredService<IAIToolRegistry>(), MicrosoftOptions.Create(new AIHostOptions()));

        var tools = await endpoint.ExecuteAsync(new Request(), CancellationToken.None);

        await Assert.That(tools).Contains(tool => tool.Name == "activities.getDescriptor");
        await Assert.That(tools).Contains(tool => tool.Name == "workflows.getDefinitionGraph");
        await Assert.That(tools).Contains(tool => tool.Name == "workflows.validateDraft");
        await Assert.That(tools).Contains(tool => tool.Name == "incidents.search");
        await Assert.That(tools).Contains(tool => tool.Name == "workflows.proposeCreate" && !tool.IsEnabled);
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

        public void Dispose()
        {
        }
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

        public void Dispose()
        {
        }
    }

    private sealed class ConstructionCounter
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Increment() => Interlocked.Increment(ref _count);
    }

    private class CountingTool : IAITool
    {
        public CountingTool(ConstructionCounter counter) => counter.Increment();

        public AIToolDefinition Definition { get; } = new()
        {
            Name = "counting.tool",
            DisplayName = "Counting tool"
        };

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIToolResult());

        public void Dispose()
        {
        }
    }

    private static void SetHttpContext(ToolsEndpoint endpoint, params string[] permissions)
    {
        var identity = new ClaimsIdentity(permissions.Select(x => new Claim(PermissionNames.ClaimType, x)), "test");
        var property = typeof(ToolsEndpoint)
            .GetProperty(nameof(ToolsEndpoint.HttpContext), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        property.SetValue(endpoint, new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        });
    }
}
