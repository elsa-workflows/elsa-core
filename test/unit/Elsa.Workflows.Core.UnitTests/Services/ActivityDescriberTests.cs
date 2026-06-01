using System.ComponentModel;
using System.Reflection;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class ActivityDescriberTests
{
    private readonly ActivityDescriber _describer;

    public ActivityDescriberTests()
    {
        var defaultValueResolver = Substitute.For<IPropertyDefaultValueResolver>();
        var propertyUIHandlerResolver = Substitute.For<IPropertyUIHandlerResolver>();
        propertyUIHandlerResolver.GetUIPropertiesAsync(Arg.Any<PropertyInfo>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<IDictionary<string, object>>(new Dictionary<string, object>()));
        _describer = new(defaultValueResolver, propertyUIHandlerResolver);
    }

    [Fact]
    public async Task DescribeActivityAsync_DerivedActivityWithoutActivityAttribute_UsesDerivedTypeIdentity()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(DerivedActivityWithoutActivityAttribute));

        Assert.Equal(typeof(DerivedActivityWithoutActivityAttribute), descriptor.ClrType);
        Assert.Equal($"{typeof(DerivedActivityWithoutActivityAttribute).Namespace}.{nameof(DerivedActivityWithoutActivityAttribute)}", descriptor.TypeName);
        Assert.Equal(nameof(DerivedActivityWithoutActivityAttribute), descriptor.Name);
        Assert.Equal("Derived Activity Without Activity Attribute", descriptor.DisplayName);
    }

    [Fact]
    public void ActivityConstructor_DerivedActivityWithoutActivityAttribute_UsesDerivedTypeIdentity()
    {
        var activity = new DerivedActivityWithoutActivityAttribute();

        Assert.Equal($"{typeof(DerivedActivityWithoutActivityAttribute).Namespace}.{nameof(DerivedActivityWithoutActivityAttribute)}", activity.Type);
    }

    [Fact]
    public async Task DescribeActivityAsync_DerivedActivity_UsesCustomizedInputOutputAndEmbeddedPortProperties()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(DerivedActivityWithCustomizedProperties));

        var input = Assert.Single(descriptor.Inputs, x => x.Name == nameof(BaseActivityWithProperties.Values));
        var output = Assert.Single(descriptor.Outputs, x => x.Name == nameof(BaseActivityWithProperties.Result));
        var port = Assert.Single(descriptor.Ports, x => x.Name == "Next");

        Assert.Equal(typeof(ICollection<string>), input.Type);
        Assert.Equal(typeof(string), output.Type);
        Assert.Equal("Continue", port.DisplayName);
        Assert.Equal(PortType.Embedded, port.Type);
    }

    [Fact]
    public async Task DescribeActivityAsync_DerivedActivity_UsesCustomizedFlowNodeOutcomes()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(DerivedActivityWithCustomizedOutcomes));

        Assert.Equal(["Approved", "Rejected"], descriptor.Ports.Select(x => x.Name));
    }

    [Activity("Base", "BaseActivity", 1, "Base description", "Base category", DisplayName = "Base Display")]
    private class BaseActivityWithActivityAttribute : Activity;

    private class DerivedActivityWithoutActivityAttribute : BaseActivityWithActivityAttribute;

    private class BaseActivityWithProperties : Activity
    {
        [Input]
        public Input<ICollection<int>> Values { get; set; } = null!;

        public Output<int> Result { get; set; } = null!;

        [Port]
        public IActivity? Next { get; set; }
    }

    private class DerivedActivityWithCustomizedProperties : BaseActivityWithProperties
    {
        [Input]
        public new Input<ICollection<string>> Values { get; set; } = null!;

        public new Output<string> Result { get; set; } = null!;

        [Port(DisplayName = "Continue")]
        public new IActivity? Next { get; set; }
    }

    [Fact]
    public async Task DescribeActivityAsync_DerivedActivityWithoutFlowNode_DoesNotInheritBaseFlowPorts()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(DerivedActivityWithoutFlowNode));

        Assert.Empty(descriptor.Ports);
    }

    [FlowNode("Done")]
    private class BaseActivityWithOutcomes : Activity;

    [FlowNode("Approved", "Rejected")]
    private class DerivedActivityWithCustomizedOutcomes : BaseActivityWithOutcomes;

    private class DerivedActivityWithoutFlowNode : BaseActivityWithOutcomes;
}
