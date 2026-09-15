using System.Reflection;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Http.UnitTests.Activities;

/// <summary>
/// Tests for <see cref="FlowSendHttpRequest"/> port/outcome declaration and descriptor modification.
/// Regression coverage for https://github.com/elsa-workflows/elsa-core/issues/7407 — subclassing
/// FlowSendHttpRequest should expose the same outcomes as the base class.
/// </summary>
public class FlowSendHttpRequestTests
{
    private static readonly string[] ExpectedStaticOutcomes =
    [
        "Done",
        "Unmatched status code",
        "Failed to connect",
        "Timeout"
    ];

    [Fact]
    public void FlowSendHttpRequest_HasFlowNodeAttribute_WithExpectedOutcomes()
    {
        var attr = typeof(FlowSendHttpRequest).GetCustomAttribute<FlowNodeAttribute>(inherit: false);

        Assert.NotNull(attr);
        foreach (var outcome in ExpectedStaticOutcomes)
            Assert.Contains(outcome, attr.Outcomes);
    }

    [Fact]
    public void SubclassOfFlowSendHttpRequest_InheritsFlowNodeAttribute()
    {
        // The attribute must be discoverable on the subclass type without the subclass
        // redeclaring it, so that ActivityDescriber (which calls GetCustomAttribute with
        // inherit:true) picks it up automatically.
        var attr = typeof(CustomFlowSendHttpRequest).GetCustomAttribute<FlowNodeAttribute>(inherit: true);

        Assert.NotNull(attr);
        foreach (var outcome in ExpectedStaticOutcomes)
            Assert.Contains(outcome, attr.Outcomes);
    }

    [Fact]
    public void SubclassOfFlowSendHttpRequest_HasNoOwnFlowNodeAttribute()
    {
        // The subclass should NOT re-declare [FlowNode] — it inherits it from the base class.
        // This test guards against accidentally hiding the base attribute.
        var ownAttr = typeof(CustomFlowSendHttpRequest).GetCustomAttribute<FlowNodeAttribute>(inherit: false);
        Assert.Null(ownAttr);
    }

    [Fact]
    public void Modifier_AddsDefaultStatusCodePort_ForFlowSendHttpRequest()
    {
        var descriptor = BuildDescriptor(typeof(FlowSendHttpRequest), new List<int> { 200 });
        var modifier = new FlowSendHttpRequestDescriptorModifier();

        modifier.Modify(descriptor);

        Assert.Contains(descriptor.Ports, p => p.Name == "200" && p.Type == PortType.Flow);
    }

    [Fact]
    public void Modifier_AddsMultipleStatusCodePorts_WhenMultipleDefaultsConfigured()
    {
        var descriptor = BuildDescriptor(typeof(FlowSendHttpRequest), new List<int> { 200, 404, 500 });
        var modifier = new FlowSendHttpRequestDescriptorModifier();

        modifier.Modify(descriptor);

        Assert.Contains(descriptor.Ports, p => p.Name == "200" && p.Type == PortType.Flow);
        Assert.Contains(descriptor.Ports, p => p.Name == "404" && p.Type == PortType.Flow);
        Assert.Contains(descriptor.Ports, p => p.Name == "500" && p.Type == PortType.Flow);
    }

    [Fact]
    public void Modifier_DoesNotAddDuplicatePort_WhenStatusCodePortAlreadyExists()
    {
        var descriptor = BuildDescriptor(typeof(FlowSendHttpRequest), new List<int> { 200 });
        descriptor.Ports.Add(new Port { Name = "200", Type = PortType.Flow });
        var modifier = new FlowSendHttpRequestDescriptorModifier();

        modifier.Modify(descriptor);

        Assert.Single(descriptor.Ports, p => p.Name == "200");
    }

    [Fact]
    public void Modifier_AddsStatusCodePort_ForSubclassOfFlowSendHttpRequest()
    {
        var descriptor = BuildDescriptor(typeof(CustomFlowSendHttpRequest), new List<int> { 201 });
        var modifier = new FlowSendHttpRequestDescriptorModifier();

        modifier.Modify(descriptor);

        Assert.Contains(descriptor.Ports, p => p.Name == "201" && p.Type == PortType.Flow);
    }

    [Fact]
    public void Modifier_DoesNotModify_WhenActivityIsNotFlowSendHttpRequest()
    {
        var descriptor = BuildDescriptor(typeof(UnrelatedActivity), null);
        var originalPortCount = descriptor.Ports.Count;
        var modifier = new FlowSendHttpRequestDescriptorModifier();

        modifier.Modify(descriptor);

        Assert.Equal(originalPortCount, descriptor.Ports.Count);
    }

    [Fact]
    public void Modifier_DoesNotModify_WhenExpectedStatusCodesInputMissing()
    {
        var descriptor = new ActivityDescriptor { ClrType = typeof(FlowSendHttpRequest) };
        var modifier = new FlowSendHttpRequestDescriptorModifier();

        modifier.Modify(descriptor);

        Assert.Empty(descriptor.Ports);
    }

    [Fact]
    public void Modifier_DoesNotModify_WhenDefaultValueIsNull()
    {
        var descriptor = BuildDescriptor(typeof(FlowSendHttpRequest), null);
        var modifier = new FlowSendHttpRequestDescriptorModifier();

        modifier.Modify(descriptor);

        Assert.Empty(descriptor.Ports);
    }

    // ---- helpers ----

    private static ActivityDescriptor BuildDescriptor(Type activityType, ICollection<int>? defaultStatusCodes)
    {
        var descriptor = new ActivityDescriptor { ClrType = activityType };

        descriptor.Inputs.Add(new InputDescriptor
        {
            Name = nameof(FlowSendHttpRequest.ExpectedStatusCodes),
            DefaultValue = defaultStatusCodes
        });

        return descriptor;
    }

    // Minimal subclass — no [FlowNode] redeclaration, replicating what a user would write.
    private class CustomFlowSendHttpRequest : FlowSendHttpRequest;

    private class UnrelatedActivity : Activity
    {
        protected override ValueTask ExecuteAsync(ActivityExecutionContext context) => ValueTask.CompletedTask;
    }
}
