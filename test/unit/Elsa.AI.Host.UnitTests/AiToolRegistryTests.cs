using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.UnitTests;

public class AiToolRegistryTests
{
    [Fact(DisplayName = "Tool registry excludes host and cross-tenant denied tools from tenant queries")]
    public async Task ToolRegistryExcludesHostAndCrossTenantDeniedToolsFromTenantQueries()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AiToolDefinition { Name = "tenant", DisplayName = "Tenant", TenantBehavior = AiTenantBehavior.TenantScoped }),
                new TestTool(new AiToolDefinition { Name = "host", DisplayName = "Host", TenantBehavior = AiTenantBehavior.HostScoped }),
                new TestTool(new AiToolDefinition { Name = "cross", DisplayName = "Cross", TenantBehavior = AiTenantBehavior.CrossTenantDenied })
            ]);

        var tools = await registry.ListAsync(new AiToolQuery
        {
            TenantId = "tenant-1",
            ActorId = "user-1"
        });

        var tool = Assert.Single(tools);
        Assert.Equal("tenant", tool.Name);
    }

    [Fact(DisplayName = "Tool registry allows cross-tenant denied tools for explicit tenant allowlists")]
    public async Task ToolRegistryAllowsCrossTenantDeniedToolsForExplicitTenantAllowlists()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AiToolDefinition
                {
                    Name = "tenant-cross",
                    DisplayName = "Tenant cross",
                    TenantBehavior = AiTenantBehavior.CrossTenantDenied,
                    TenantIds = ["tenant-1"]
                })
            ]);

        var tools = await registry.ListAsync(new AiToolQuery
        {
            TenantId = "tenant-1",
            ActorId = "user-1"
        });

        var tool = Assert.Single(tools);
        Assert.Equal("tenant-cross", tool.Name);
    }

    [Fact(DisplayName = "Tool registry excludes cross-tenant denied tools from host queries")]
    public async Task ToolRegistryExcludesCrossTenantDeniedToolsFromHostQueries()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AiToolDefinition { Name = "host", DisplayName = "Host", TenantBehavior = AiTenantBehavior.HostScoped }),
                new TestTool(new AiToolDefinition { Name = "cross", DisplayName = "Cross", TenantBehavior = AiTenantBehavior.CrossTenantDenied })
            ]);

        var tools = await registry.ListAsync(new AiToolQuery
        {
            ActorId = "user-1"
        });

        var tool = Assert.Single(tools);
        Assert.Equal("host", tool.Name);
    }

    [Fact(DisplayName = "Tool registry exposes default tools without tenant context")]
    public async Task ToolRegistryExposesDefaultToolsWithoutTenantContext()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AiToolDefinition { Name = "default", DisplayName = "Default" })
            ]);

        var tools = await registry.ListAsync(new AiToolQuery { ActorId = "user-1" });

        var tool = Assert.Single(tools);
        Assert.Equal("default", tool.Name);
    }

    [Fact(DisplayName = "Tool registry uses cached concrete type lookup after listing tools")]
    public async Task ToolRegistryUsesCachedConcreteTypeLookupAfterListingTools()
    {
        CountingTool.ConstructionCount = 0;
        OtherCountingTool.ConstructionCount = 0;
        var services = new ServiceCollection();
        services.AddTransient<IAiTool, CountingTool>();
        services.AddTransient<IAiTool, OtherCountingTool>();
        var registry = CreateRegistry(services);

        await registry.ListAsync(new AiToolQuery { ActorId = "user-1" });
        CountingTool.ConstructionCount = 0;
        OtherCountingTool.ConstructionCount = 0;

        var tool = await registry.FindAsync("counting", new AiToolQuery { ActorId = "user-1" });

        Assert.NotNull(tool);
        Assert.Equal(1, CountingTool.ConstructionCount);
        Assert.Equal(0, OtherCountingTool.ConstructionCount);
    }

    [Fact(DisplayName = "Tool registry find applies tenant and actor filters")]
    public async Task ToolRegistryFindAppliesTenantAndActorFilters()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AiToolDefinition
                {
                    Name = "restricted",
                    DisplayName = "Restricted",
                    EnabledByDefault = true,
                    TenantBehavior = AiTenantBehavior.TenantScoped,
                    TenantIds = ["tenant-1"],
                    ActorIds = ["user-1"]
                })
            ]);

        var allowed = await registry.FindAsync("restricted", new AiToolQuery { TenantId = "tenant-1", ActorId = "user-1" });
        var denied = await registry.FindAsync("restricted", new AiToolQuery { TenantId = "tenant-2", ActorId = "user-1" });

        Assert.NotNull(allowed);
        Assert.Null(denied);
    }

    [Fact(DisplayName = "Tool registry enforces tool permission requirements")]
    public async Task ToolRegistryEnforcesToolPermissionRequirements()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AiToolDefinition
                {
                    Name = "restricted",
                    DisplayName = "Restricted",
                    EnabledByDefault = true,
                    TenantBehavior = AiTenantBehavior.TenantScoped,
                    Permissions = ["workflows:write"]
                })
            ]);

        var denied = await registry.FindAsync("restricted", new AiToolQuery { TenantId = "tenant-1", ActorId = "user-1" });
        var allowed = await registry.FindAsync("restricted", new AiToolQuery
        {
            TenantId = "tenant-1",
            ActorId = "user-1",
            UserPermissions = ["Workflows:Write"]
        });

        Assert.Null(denied);
        Assert.NotNull(allowed);
    }

    [Fact(DisplayName = "Tool registry filters agent-scoped tools by agent")]
    public async Task ToolRegistryFiltersAgentScopedToolsByAgent()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AiToolDefinition
                {
                    Name = "agent-only",
                    DisplayName = "Agent only",
                    EnabledByDefault = true,
                    TenantBehavior = AiTenantBehavior.TenantScoped,
                    AgentScopes = ["workflow-author"]
                }),
                new TestTool(new AiToolDefinition
                {
                    Name = "agent-permission",
                    DisplayName = "Agent permission",
                    EnabledByDefault = true,
                    TenantBehavior = AiTenantBehavior.TenantScoped,
                    AgentScopes = ["workflow-author"],
                    Permissions = ["workflows:write"]
                })
            ]);

        var deniedWithoutPermission = await registry.ListAsync(new AiToolQuery
        {
            Agent = "workflow-author",
            TenantId = "tenant-1",
            ActorId = "user-1"
        });
        var deniedByAgent = await registry.ListAsync(new AiToolQuery
        {
            Agent = "workflow-editor",
            TenantId = "tenant-1",
            ActorId = "user-1",
            UserPermissions = ["workflows:write"]
        });
        var allowedWithPermission = await registry.ListAsync(new AiToolQuery
        {
            Agent = "workflow-author",
            TenantId = "tenant-1",
            ActorId = "user-1",
            UserPermissions = ["workflows:write"]
        });

        var tool = Assert.Single(deniedWithoutPermission);
        Assert.Equal("agent-only", tool.Name);
        Assert.Empty(deniedByAgent);
        Assert.Collection(
            allowedWithPermission.OrderBy(x => x.Name),
            agentOnly => Assert.Equal("agent-only", agentOnly.Name),
            agentPermission => Assert.Equal("agent-permission", agentPermission.Name));
    }

    [Fact(DisplayName = "Tool registry honors tenant and actor allowlists")]
    public async Task ToolRegistryHonorsTenantAndActorAllowlists()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AiToolDefinition
                {
                    Name = "matching",
                    DisplayName = "Matching",
                    TenantBehavior = AiTenantBehavior.TenantScoped,
                    TenantIds = ["tenant-1"],
                    ActorIds = ["user-1"]
                }),
                new TestTool(new AiToolDefinition
                {
                    Name = "wrong-tenant",
                    DisplayName = "Wrong tenant",
                    TenantBehavior = AiTenantBehavior.TenantScoped,
                    TenantIds = ["tenant-2"]
                }),
                new TestTool(new AiToolDefinition
                {
                    Name = "wrong-actor",
                    DisplayName = "Wrong actor",
                    ActorIds = ["user-2"]
                })
            ]);

        var tools = await registry.ListAsync(new AiToolQuery
        {
            TenantId = "tenant-1",
            ActorId = "user-1"
        });

        var tool = Assert.Single(tools);
        Assert.Equal("matching", tool.Name);
    }

    [Fact(DisplayName = "Tool registry disposes find scope when tool resolution throws")]
    public async Task ToolRegistryDisposesFindScopeWhenToolResolutionThrows()
    {
        var tracker = new ScopeDisposalTracker();
        var services = new ServiceCollection();
        services.AddSingleton(tracker);
        services.AddScoped<ScopedDependency>();
        services.AddScoped<IAiTool, ThrowingDefinitionTool>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var registry = new AiToolRegistry(provider.GetRequiredService<IServiceScopeFactory>(), new AiToolEnablementService());

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await registry.FindAsync("throwing", new AiToolQuery()));

        Assert.Equal(1, tracker.DisposeCount);
    }

    private static AiToolRegistry CreateRegistry(IReadOnlyCollection<IAiTool> tools)
    {
        var services = new ServiceCollection();
        foreach (var tool in tools)
            services.AddScoped<IAiTool>(_ => tool);

        return CreateRegistry(services);
    }

    private static AiToolRegistry CreateRegistry(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        return new AiToolRegistry(provider.GetRequiredService<IServiceScopeFactory>(), new AiToolEnablementService());
    }

    private class TestTool(AiToolDefinition definition) : IAiTool
    {
        public AiToolDefinition Definition { get; } = definition;

        public ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiToolResult());
    }

    private class CountingTool : IAiTool
    {
        public static int ConstructionCount { get; set; }

        public CountingTool()
        {
            ConstructionCount++;
        }

        public AiToolDefinition Definition { get; } = new()
        {
            Name = "counting",
            DisplayName = "Counting",
            EnabledByDefault = true
        };

        public ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiToolResult());
    }

    private class OtherCountingTool : IAiTool
    {
        public static int ConstructionCount { get; set; }

        public OtherCountingTool()
        {
            ConstructionCount++;
        }

        public AiToolDefinition Definition { get; } = new()
        {
            Name = "other-counting",
            DisplayName = "Other counting",
            EnabledByDefault = true
        };

        public ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiToolResult());
    }

    private class ThrowingDefinitionTool(ScopedDependency dependency) : IAiTool
    {
        private readonly ScopedDependency _dependency = dependency;

        public AiToolDefinition Definition
        {
            get
            {
                _ = _dependency;
                throw new InvalidOperationException("Definition unavailable.");
            }
        }

        public ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiToolResult());
    }

    private class ScopedDependency(ScopeDisposalTracker tracker) : IDisposable
    {
        public void Dispose()
        {
            tracker.DisposeCount++;
        }
    }

    private class ScopeDisposalTracker
    {
        public int DisposeCount { get; set; }
    }
}
