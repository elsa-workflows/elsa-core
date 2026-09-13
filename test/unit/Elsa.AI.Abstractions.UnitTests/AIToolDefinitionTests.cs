
using Elsa.AI.Abstractions.Models;
using System.Threading.Tasks;

namespace Elsa.AI.Abstractions.UnitTests;

public class AIToolDefinitionTests
{
    [Test]
    [DisplayName("Tool definitions carry governance metadata")]
    public async Task ToolDefinitionsCarryGovernanceMetadata()
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

        await Assert.That(definition.Name).IsEqualTo("workflow.getDefinition");
        await Assert.That(definition.Mutability).IsEqualTo(AIToolMutability.ReadOnly);
        await Assert.That(definition.DangerLevel).IsEqualTo(AIToolDangerLevel.Low);
        await Assert.That(definition.Permissions).Contains("read:workflows");
        await Assert.That(definition.EnabledByDefault).IsTrue();
    }

    [Test]
    [DisplayName("Tool definitions default to tenant scoped visibility")]
    public async Task ToolDefinitionsDefaultToTenantScopedVisibility()
    {
        var definition = new AIToolDefinition
        {
            Name = "workflow.getDefinition",
            DisplayName = "Get workflow definition"
        };

        await Assert.That(definition.TenantBehavior).IsEqualTo(AITenantBehavior.TenantScoped);
    }

    [Test]
    [DisplayName("Audit events default string fields to non-null values")]
    public async Task AuditEventsDefaultStringFieldsToNonNullValues()
    {
        var auditEvent = new AIAuditEvent();

        await Assert.That(string.IsNullOrWhiteSpace(auditEvent.Id)).IsFalse();
        await Assert.That(auditEvent.ActorId).IsEqualTo("");
        await Assert.That(auditEvent.Type).IsEqualTo("");
        await Assert.That(auditEvent.Timestamp).IsNotEqualTo(default);
    }
}