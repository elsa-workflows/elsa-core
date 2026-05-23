using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Abstractions.UnitTests;

public class AIToolDefinitionTests
{
    [Fact(DisplayName = "Tool definitions carry governance metadata")]
    public void ToolDefinitionsCarryGovernanceMetadata()
    {
        var definition = new AIToolDefinition
        {
            Name = "workflow.getDefinition",
            DisplayName = "Get workflow definition",
            Mutability = AIToolMutability.ReadOnly,
            DangerLevel = AIToolDangerLevel.Low,
            Permissions = ["read:workflows"],
            TenantBehavior = AITenantBehavior.TenantScoped,
            AuditBehavior = AIToolAuditBehavior.RecordInvocation,
            EnabledByDefault = true
        };

        Assert.Equal("workflow.getDefinition", definition.Name);
        Assert.Equal(AIToolMutability.ReadOnly, definition.Mutability);
        Assert.Equal(AIToolDangerLevel.Low, definition.DangerLevel);
        Assert.Contains("read:workflows", definition.Permissions);
        Assert.True(definition.EnabledByDefault);
    }

    [Fact(DisplayName = "Tool definitions default to tenant scoped visibility")]
    public void ToolDefinitionsDefaultToTenantScopedVisibility()
    {
        var definition = new AIToolDefinition
        {
            Name = "workflow.getDefinition",
            DisplayName = "Get workflow definition"
        };

        Assert.Equal(AITenantBehavior.TenantScoped, definition.TenantBehavior);
    }

    [Fact(DisplayName = "Audit events default string fields to non-null values")]
    public void AuditEventsDefaultStringFieldsToNonNullValues()
    {
        var auditEvent = new AIAuditEvent();

        Assert.False(string.IsNullOrWhiteSpace(auditEvent.Id));
        Assert.Equal("", auditEvent.ActorId);
        Assert.Equal("", auditEvent.Type);
    }
}
