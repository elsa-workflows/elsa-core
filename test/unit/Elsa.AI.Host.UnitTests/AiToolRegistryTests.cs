using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;

namespace Elsa.AI.Host.UnitTests;

public class AiToolRegistryTests
{
    [Fact(DisplayName = "Tool registry excludes host and cross-tenant denied tools from tenant queries")]
    public async Task ToolRegistryExcludesHostAndCrossTenantDeniedToolsFromTenantQueries()
    {
        var registry = new AiToolRegistry(
            [
                new TestTool(new AiToolDefinition { Name = "tenant", DisplayName = "Tenant", TenantBehavior = AiTenantBehavior.TenantScoped }),
                new TestTool(new AiToolDefinition { Name = "host", DisplayName = "Host", TenantBehavior = AiTenantBehavior.HostScoped }),
                new TestTool(new AiToolDefinition { Name = "cross", DisplayName = "Cross", TenantBehavior = AiTenantBehavior.CrossTenantDenied })
            ],
            new AiToolEnablementService());

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
        var registry = new AiToolRegistry(
            [
                new TestTool(new AiToolDefinition
                {
                    Name = "tenant-cross",
                    DisplayName = "Tenant cross",
                    TenantBehavior = AiTenantBehavior.CrossTenantDenied,
                    TenantIds = ["tenant-1"]
                })
            ],
            new AiToolEnablementService());

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
        var registry = new AiToolRegistry(
            [
                new TestTool(new AiToolDefinition { Name = "host", DisplayName = "Host", TenantBehavior = AiTenantBehavior.HostScoped }),
                new TestTool(new AiToolDefinition { Name = "cross", DisplayName = "Cross", TenantBehavior = AiTenantBehavior.CrossTenantDenied })
            ],
            new AiToolEnablementService());

        var tools = await registry.ListAsync(new AiToolQuery
        {
            ActorId = "user-1"
        });

        var tool = Assert.Single(tools);
        Assert.Equal("host", tool.Name);
    }

    [Fact(DisplayName = "Tool registry find applies tenant and actor filters")]
    public async Task ToolRegistryFindAppliesTenantAndActorFilters()
    {
        var registry = new AiToolRegistry(
            [
                new TestTool(new AiToolDefinition
                {
                    Name = "restricted",
                    DisplayName = "Restricted",
                    EnabledByDefault = true,
                    TenantIds = ["tenant-1"],
                    ActorIds = ["user-1"]
                })
            ],
            new AiToolEnablementService());

        var allowed = await registry.FindAsync("restricted", new AiToolQuery { TenantId = "tenant-1", ActorId = "user-1" });
        var denied = await registry.FindAsync("restricted", new AiToolQuery { TenantId = "tenant-2", ActorId = "user-1" });

        Assert.NotNull(allowed);
        Assert.Null(denied);
    }

    [Fact(DisplayName = "Tool registry enforces tool permission requirements")]
    public async Task ToolRegistryEnforcesToolPermissionRequirements()
    {
        var registry = new AiToolRegistry(
            [
                new TestTool(new AiToolDefinition
                {
                    Name = "restricted",
                    DisplayName = "Restricted",
                    EnabledByDefault = true,
                    Permissions = ["workflows:write"]
                })
            ],
            new AiToolEnablementService());

        var denied = await registry.FindAsync("restricted", new AiToolQuery { TenantId = "tenant-1", ActorId = "user-1" });
        var allowed = await registry.FindAsync("restricted", new AiToolQuery
        {
            TenantId = "tenant-1",
            ActorId = "user-1",
            UserPermissions = ["workflows:write"]
        });

        Assert.Null(denied);
        Assert.NotNull(allowed);
    }

    [Fact(DisplayName = "Tool registry honors tenant and actor allowlists")]
    public async Task ToolRegistryHonorsTenantAndActorAllowlists()
    {
        var registry = new AiToolRegistry(
            [
                new TestTool(new AiToolDefinition
                {
                    Name = "matching",
                    DisplayName = "Matching",
                    TenantIds = ["tenant-1"],
                    ActorIds = ["user-1"]
                }),
                new TestTool(new AiToolDefinition
                {
                    Name = "wrong-tenant",
                    DisplayName = "Wrong tenant",
                    TenantIds = ["tenant-2"]
                }),
                new TestTool(new AiToolDefinition
                {
                    Name = "wrong-actor",
                    DisplayName = "Wrong actor",
                    ActorIds = ["user-2"]
                })
            ],
            new AiToolEnablementService());

        var tools = await registry.ListAsync(new AiToolQuery
        {
            TenantId = "tenant-1",
            ActorId = "user-1"
        });

        var tool = Assert.Single(tools);
        Assert.Equal("matching", tool.Name);
    }

    private class TestTool(AiToolDefinition definition) : IAiTool
    {
        public AiToolDefinition Definition { get; } = definition;

        public ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new AiToolResult());
    }
}
