using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Host.UnitTests;

public class AIToolRegistryTests
{
    [Fact(DisplayName = "Tool registry excludes host and cross-tenant denied tools from tenant queries")]
    public async Task ToolRegistryExcludesHostAndCrossTenantDeniedToolsFromTenantQueries()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AIToolDefinition { Name = "tenant", DisplayName = "Tenant", TenantBehavior = AITenantBehavior.TenantScoped }),
                new TestTool(new AIToolDefinition { Name = "host", DisplayName = "Host", TenantBehavior = AITenantBehavior.HostScoped }),
                new TestTool(new AIToolDefinition { Name = "cross", DisplayName = "Cross", TenantBehavior = AITenantBehavior.CrossTenantDenied })
            ]);

        var tools = await registry.ListAsync(new AIToolQuery
        {
            TenantId = "tenant-1",
            ActorId = "user-1"
        });

        var tool = Assert.Single(tools);
        Assert.Equal("tenant", tool.Name);
    }

    [Fact(DisplayName = "Tool registry treats empty tenant ID as tenant context")]
    public async Task ToolRegistryTreatsEmptyTenantIdAsTenantContext()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AIToolDefinition { Name = "tenant", DisplayName = "Tenant", TenantBehavior = AITenantBehavior.TenantScoped }),
                new TestTool(new AIToolDefinition { Name = "host", DisplayName = "Host", TenantBehavior = AITenantBehavior.HostScoped }),
                new TestTool(new AIToolDefinition
                {
                    Name = "default-tenant-cross",
                    DisplayName = "Default tenant cross",
                    TenantBehavior = AITenantBehavior.CrossTenantDenied,
                    TenantIds = [""]
                })
            ]);

        var tools = await registry.ListAsync(new AIToolQuery
        {
            TenantId = "",
            ActorId = "user-1"
        });

        Assert.Collection(
            tools.OrderBy(x => x.Name),
            tool => Assert.Equal("default-tenant-cross", tool.Name),
            tool => Assert.Equal("tenant", tool.Name));
    }

    [Fact(DisplayName = "Tool registry allows cross-tenant denied tools for explicit tenant allowlists")]
    public async Task ToolRegistryAllowsCrossTenantDeniedToolsForExplicitTenantAllowlists()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AIToolDefinition
                {
                    Name = "tenant-cross",
                    DisplayName = "Tenant cross",
                    TenantBehavior = AITenantBehavior.CrossTenantDenied,
                    TenantIds = ["tenant-1"]
                })
            ]);

        var tools = await registry.ListAsync(new AIToolQuery
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
                new TestTool(new AIToolDefinition { Name = "host", DisplayName = "Host", TenantBehavior = AITenantBehavior.HostScoped }),
                new TestTool(new AIToolDefinition { Name = "cross", DisplayName = "Cross", TenantBehavior = AITenantBehavior.CrossTenantDenied })
            ]);

        var tools = await registry.ListAsync(new AIToolQuery
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
                new TestTool(new AIToolDefinition { Name = "default", DisplayName = "Default" })
            ]);

        var tools = await registry.ListAsync(new AIToolQuery { ActorId = "user-1" });

        var tool = Assert.Single(tools);
        Assert.Equal("default", tool.Name);
    }

    [Fact(DisplayName = "Tool registry exposes default tools to tenant queries")]
    public async Task ToolRegistryExposesDefaultToolsToTenantQueries()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AIToolDefinition { Name = "default", DisplayName = "Default" })
            ]);

        var tools = await registry.ListAsync(new AIToolQuery { TenantId = "tenant-1", ActorId = "user-1" });

        var tool = Assert.Single(tools);
        Assert.Equal("default", tool.Name);
    }

    [Fact(DisplayName = "Tool registry excludes incomplete tool definition names")]
    public async Task ToolRegistryExcludesIncompleteToolDefinitionNames()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AIToolDefinition { DisplayName = "Incomplete" }),
                new TestTool(new AIToolDefinition { Name = "valid", DisplayName = "Valid" })
            ]);

        var tools = await registry.ListAsync(new AIToolQuery { ActorId = "user-1" });

        var tool = Assert.Single(tools);
        Assert.Equal("valid", tool.Name);
    }

    [Fact(DisplayName = "Tool registry uses cached concrete type lookup after listing tools")]
    public async Task ToolRegistryUsesCachedConcreteTypeLookupAfterListingTools()
    {
        CountingTool.ConstructionCount = 0;
        OtherCountingTool.ConstructionCount = 0;
        var services = new ServiceCollection();
        services.AddTransient<IAITool, CountingTool>();
        services.AddTransient<IAITool, OtherCountingTool>();
        var registry = CreateRegistry(services);

        await registry.ListAsync(new AIToolQuery { ActorId = "user-1" });
        CountingTool.ConstructionCount = 0;
        OtherCountingTool.ConstructionCount = 0;

        using var tool = await registry.FindAsync("counting", new AIToolQuery { ActorId = "user-1" });

        Assert.NotNull(tool);
        Assert.Equal(1, CountingTool.ConstructionCount);
        Assert.Equal(0, OtherCountingTool.ConstructionCount);
    }

    [Fact(DisplayName = "Tool registry disposes cached concrete tool instances")]
    public async Task ToolRegistryDisposesCachedConcreteToolInstances()
    {
        DisposableCachedTool.ConstructionCount = 0;
        DisposableCachedTool.DisposeCount = 0;
        var services = new ServiceCollection();
        services.AddTransient<IAITool, DisposableCachedTool>();
        var registry = CreateRegistry(services);

        await registry.ListAsync(new AIToolQuery { ActorId = "user-1" });
        DisposableCachedTool.ConstructionCount = 0;
        DisposableCachedTool.DisposeCount = 0;

        var tool = await registry.FindAsync("disposable-cached", new AIToolQuery { ActorId = "user-1" });
        tool?.Dispose();

        Assert.NotNull(tool);
        Assert.Equal(1, DisposableCachedTool.ConstructionCount);
        Assert.Equal(1, DisposableCachedTool.DisposeCount);
    }

    [Fact(DisplayName = "Tool registry find applies tenant and actor filters")]
    public async Task ToolRegistryFindAppliesTenantAndActorFilters()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AIToolDefinition
                {
                    Name = "restricted",
                    DisplayName = "Restricted",
                    EnabledByDefault = true,
                    TenantBehavior = AITenantBehavior.TenantScoped,
                    TenantIds = ["tenant-1"],
                    ActorIds = ["user-1"]
                })
            ]);

        using var allowed = await registry.FindAsync("restricted", new AIToolQuery { TenantId = "tenant-1", ActorId = "user-1" });
        var denied = await registry.FindAsync("restricted", new AIToolQuery { TenantId = "tenant-2", ActorId = "user-1" });

        Assert.NotNull(allowed);
        Assert.Null(denied);
    }

    [Fact(DisplayName = "Tool registry enforces tool permission requirements")]
    public async Task ToolRegistryEnforcesToolPermissionRequirements()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AIToolDefinition
                {
                    Name = "restricted",
                    DisplayName = "Restricted",
                    EnabledByDefault = true,
                    TenantBehavior = AITenantBehavior.TenantScoped,
                    Permissions = ["workflows:write"]
                })
            ]);

        var denied = await registry.FindAsync("restricted", new AIToolQuery { TenantId = "tenant-1", ActorId = "user-1" });
        using var allowed = await registry.FindAsync("restricted", new AIToolQuery
        {
            TenantId = "tenant-1",
            ActorId = "user-1",
            UserPermissions = ["Workflows:Write"]
        });

        Assert.Null(denied);
        Assert.NotNull(allowed);
    }

    [Fact(DisplayName = "Tool enablement requires explicit administrative enablement")]
    public void ToolEnablementRequiresExplicitAdministrativeEnablement()
    {
        var enablement = new AIToolEnablementService();
        var definition = new AIToolDefinition
        {
            Name = "admin",
            DisplayName = "Admin",
            Mutability = AIToolMutability.Administrative,
            EnabledByDefault = true
        };

        Assert.False(enablement.IsEnabled(definition));

        enablement.Enable("admin");
        Assert.False(enablement.IsEnabled(definition));

        enablement.EnableAdministrative("admin");
        Assert.True(enablement.IsEnabled(definition));
    }

    [Fact(DisplayName = "Tool enablement disables administrative tools by name")]
    public void ToolEnablementDisablesAdministrativeToolsByName()
    {
        var enablement = new AIToolEnablementService();
        var definition = new AIToolDefinition
        {
            Name = "admin",
            DisplayName = "Admin",
            Mutability = AIToolMutability.Administrative
        };

        enablement.EnableAdministrative("ADMIN");
        Assert.True(enablement.IsEnabled(definition));

        enablement.Disable("admin");

        Assert.False(enablement.IsEnabled(definition));
    }

    [Fact(DisplayName = "Tool registry filters agent-scoped tools by agent")]
    public async Task ToolRegistryFiltersAgentScopedToolsByAgent()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AIToolDefinition
                {
                    Name = "agent-only",
                    DisplayName = "Agent only",
                    EnabledByDefault = true,
                    TenantBehavior = AITenantBehavior.TenantScoped,
                    AgentScopes = ["workflow-author"]
                }),
                new TestTool(new AIToolDefinition
                {
                    Name = "agent-permission",
                    DisplayName = "Agent permission",
                    EnabledByDefault = true,
                    TenantBehavior = AITenantBehavior.TenantScoped,
                    AgentScopes = ["workflow-author"],
                    Permissions = ["workflows:write"]
                })
            ]);

        var deniedWithoutPermission = await registry.ListAsync(new AIToolQuery
        {
            Agent = "workflow-author",
            TenantId = "tenant-1",
            ActorId = "user-1"
        });
        var deniedByAgent = await registry.ListAsync(new AIToolQuery
        {
            Agent = "workflow-editor",
            TenantId = "tenant-1",
            ActorId = "user-1",
            UserPermissions = ["workflows:write"]
        });
        var allowedWithPermission = await registry.ListAsync(new AIToolQuery
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
                new TestTool(new AIToolDefinition
                {
                    Name = "matching",
                    DisplayName = "Matching",
                    TenantBehavior = AITenantBehavior.TenantScoped,
                    TenantIds = ["tenant-1"],
                    ActorIds = ["user-1"]
                }),
                new TestTool(new AIToolDefinition
                {
                    Name = "wrong-tenant",
                    DisplayName = "Wrong tenant",
                    TenantBehavior = AITenantBehavior.TenantScoped,
                    TenantIds = ["tenant-2"]
                }),
                new TestTool(new AIToolDefinition
                {
                    Name = "wrong-actor",
                    DisplayName = "Wrong actor",
                    ActorIds = ["user-2"]
                })
            ]);

        var tools = await registry.ListAsync(new AIToolQuery
        {
            TenantId = "tenant-1",
            ActorId = "user-1"
        });

        var tool = Assert.Single(tools);
        Assert.Equal("matching", tool.Name);
    }

    [Fact(DisplayName = "Tool registry combines tenant actor agent and permission filters")]
    public async Task ToolRegistryCombinesTenantActorAgentAndPermissionFilters()
    {
        var registry = CreateRegistry(
            [
                new TestTool(new AIToolDefinition
                {
                    Name = "matching",
                    DisplayName = "Matching",
                    TenantBehavior = AITenantBehavior.TenantScoped,
                    TenantIds = ["tenant-1"],
                    ActorIds = ["user-1"],
                    AgentScopes = ["workflow-author"],
                    Permissions = ["workflows:write"]
                }),
                new TestTool(new AIToolDefinition
                {
                    Name = "wrong-agent",
                    DisplayName = "Wrong agent",
                    TenantBehavior = AITenantBehavior.TenantScoped,
                    TenantIds = ["tenant-1"],
                    ActorIds = ["user-1"],
                    AgentScopes = ["workflow-editor"],
                    Permissions = ["workflows:write"]
                }),
                new TestTool(new AIToolDefinition
                {
                    Name = "wrong-permission",
                    DisplayName = "Wrong permission",
                    TenantBehavior = AITenantBehavior.TenantScoped,
                    TenantIds = ["tenant-1"],
                    ActorIds = ["user-1"],
                    AgentScopes = ["workflow-author"],
                    Permissions = ["deployments:write"]
                })
            ]);

        var tools = await registry.ListAsync(new AIToolQuery
        {
            TenantId = "tenant-1",
            ActorId = "user-1",
            Agent = "workflow-author",
            UserPermissions = ["workflows:write"]
        });

        var tool = Assert.Single(tools);
        Assert.Equal("matching", tool.Name);
    }

    [Fact(DisplayName = "Tool registry skips tools with throwing definitions")]
    public async Task ToolRegistrySkipsToolsWithThrowingDefinitions()
    {
        var tracker = new ScopeDisposalTracker();
        var services = new ServiceCollection();
        services.AddSingleton(tracker);
        services.AddScoped<ScopedDependency>();
        services.AddScoped<IAITool, ThrowingDefinitionTool>();
        services.AddScoped<IAITool>(_ => new TestTool(new AIToolDefinition { Name = "healthy", DisplayName = "Healthy" }));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var registry = new AIToolRegistry(provider.GetRequiredService<IServiceScopeFactory>(), new AIToolEnablementService());

        var missing = await registry.FindAsync("throwing", new AIToolQuery());
        var tools = await registry.ListAsync(new AIToolQuery());

        Assert.Null(missing);
        var tool = Assert.Single(tools);
        Assert.Equal("healthy", tool.Name);
        Assert.Equal(2, tracker.DisposeCount);
    }

    private static AIToolRegistry CreateRegistry(IReadOnlyCollection<IAITool> tools)
    {
        var services = new ServiceCollection();
        foreach (var tool in tools)
            services.AddScoped<IAITool>(_ => tool);

        return CreateRegistry(services);
    }

    private static AIToolRegistry CreateRegistry(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        return new AIToolRegistry(provider.GetRequiredService<IServiceScopeFactory>(), new AIToolEnablementService());
    }

    private class TestTool(AIToolDefinition definition) : IAITool
    {
        public AIToolDefinition Definition { get; } = definition;

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIToolResult());

        public void Dispose()
        {
        }
    }

    private class CountingTool : IAITool
    {
        public static int ConstructionCount { get; set; }

        public CountingTool()
        {
            ConstructionCount++;
        }

        public AIToolDefinition Definition { get; } = new()
        {
            Name = "counting",
            DisplayName = "Counting",
            EnabledByDefault = true
        };

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIToolResult());

        public void Dispose()
        {
        }
    }

    private class OtherCountingTool : IAITool
    {
        public static int ConstructionCount { get; set; }

        public OtherCountingTool()
        {
            ConstructionCount++;
        }

        public AIToolDefinition Definition { get; } = new()
        {
            Name = "other-counting",
            DisplayName = "Other counting",
            EnabledByDefault = true
        };

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIToolResult());

        public void Dispose()
        {
        }
    }

    private class DisposableCachedTool : IAITool
    {
        public static int ConstructionCount { get; set; }
        public static int DisposeCount { get; set; }

        public DisposableCachedTool()
        {
            ConstructionCount++;
        }

        public AIToolDefinition Definition { get; } = new()
        {
            Name = "disposable-cached",
            DisplayName = "Disposable cached",
            EnabledByDefault = true
        };

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIToolResult());

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private class ThrowingDefinitionTool(ScopedDependency dependency) : IAITool
    {
        private readonly ScopedDependency _dependency = dependency;

        public AIToolDefinition Definition
        {
            get
            {
                _ = _dependency;
                throw new InvalidOperationException("Definition unavailable.");
            }
        }

        public ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AIToolResult());

        public void Dispose()
        {
        }
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
