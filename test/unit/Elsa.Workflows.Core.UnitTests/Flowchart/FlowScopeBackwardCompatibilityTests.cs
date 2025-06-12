using System.Text.Json;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Core.UnitTests.Flowchart;

/// <summary>
/// Tests for FlowScope backward compatibility with older versions.
/// </summary>
public class FlowScopeBackwardCompatibilityTests
{
    [Fact(DisplayName = "FlowScope should have OwnerActivityId property for backward compatibility")]
    public void FlowScope_Should_Have_OwnerActivityId_Property()
    {
        // Arrange & Act
        var flowScope = new FlowScope();
        
        // Assert - Property should exist and be settable
        Assert.NotNull(flowScope);
        
        // This should not throw even though the property is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        flowScope.OwnerActivityId = "test-activity-id";
        Assert.Equal("test-activity-id", flowScope.OwnerActivityId);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact(DisplayName = "OwnerActivityId should be ignored in JSON serialization")]
    public void OwnerActivityId_Should_Be_Ignored_In_Json_Serialization()
    {
        // Arrange
        var flowScope = new FlowScope();
        
#pragma warning disable CS0618 // Type or member is obsolete
        flowScope.OwnerActivityId = "test-activity-id";
#pragma warning restore CS0618 // Type or member is obsolete

        // Act
        var json = JsonSerializer.Serialize(flowScope);

        // Assert - The serialized JSON should not contain OwnerActivityId
        Assert.DoesNotContain("OwnerActivityId", json);
        Assert.DoesNotContain("test-activity-id", json);
    }

    [Fact(DisplayName = "FlowScope should deserialize without OwnerActivityId")]
    public void FlowScope_Should_Deserialize_Without_OwnerActivityId()
    {
        // Arrange
        var json = "{}";

        // Act
        var flowScope = JsonSerializer.Deserialize<FlowScope>(json);

        // Assert
        Assert.NotNull(flowScope);
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Null(flowScope.OwnerActivityId);
#pragma warning restore CS0618 // Type or member is obsolete
    }
}