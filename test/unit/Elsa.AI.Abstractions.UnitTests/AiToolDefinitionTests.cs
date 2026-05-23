using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Abstractions.UnitTests;

public class AiToolDefinitionTests
{
    [Fact(DisplayName = "Tool definitions carry governance metadata")]
    public void ToolDefinitionsCarryGovernanceMetadata()
    {
        var definition = new AiToolDefinition
        {
            Name = "workflow.getDefinition",
            DisplayName = "Get workflow definition",
            Mutability = AiToolMutability.ReadOnly,
            DangerLevel = AiToolDangerLevel.Low,
            Permissions = ["read:workflows"],
            TenantBehavior = AiTenantBehavior.TenantScoped,
            AuditBehavior = AiToolAuditBehavior.RecordInvocation,
            EnabledByDefault = true
        };

        Assert.Equal("workflow.getDefinition", definition.Name);
        Assert.Equal(AiToolMutability.ReadOnly, definition.Mutability);
        Assert.Equal(AiToolDangerLevel.Low, definition.DangerLevel);
        Assert.Contains("read:workflows", definition.Permissions);
        Assert.True(definition.EnabledByDefault);
    }

    [Fact(DisplayName = "Tool definitions default to host scoped visibility")]
    public void ToolDefinitionsDefaultToHostScopedVisibility()
    {
        var definition = new AiToolDefinition
        {
            Name = "workflow.getDefinition",
            DisplayName = "Get workflow definition"
        };

        Assert.Equal(AiTenantBehavior.HostScoped, definition.TenantBehavior);
    }
}
