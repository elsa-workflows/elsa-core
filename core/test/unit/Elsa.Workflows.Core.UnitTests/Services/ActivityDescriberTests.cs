using System.Reflection;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using NSubstitute;
using Xunit;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class ActivityDescriberTests
{
    private readonly IActivityDescriber _describer;

    public ActivityDescriberTests()
    {
        var defaultValueResolver = Substitute.For<IPropertyDefaultValueResolver>();
        var propertyUIHandlerResolver = Substitute.For<IPropertyUIHandlerResolver>();

        defaultValueResolver.GetDefaultValue(Arg.Any<PropertyInfo>()).Returns((object?)null);
        propertyUIHandlerResolver
            .GetUIPropertiesAsync(Arg.Any<PropertyInfo>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<IDictionary<string, object>>(new Dictionary<string, object>()));

        _describer = new ActivityDescriber(defaultValueResolver, propertyUIHandlerResolver);
    }

    [Fact]
    public async Task DescribeActivityAsync_MapsCanContainSecretsToInputDescriptorSensitivity()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(ActivityWithSensitiveInputs));

        var sensitiveInput = descriptor.Inputs.Single(x => x.Name == nameof(ActivityWithSensitiveInputs.SensitiveText));
        var publicInput = descriptor.Inputs.Single(x => x.Name == nameof(ActivityWithSensitiveInputs.PublicText));

        Assert.True(sensitiveInput.IsSensitive);
        Assert.False(publicInput.IsSensitive);
    }

    [Fact]
    public async Task DescribeActivityAsync_FormatsGenericTypeFriendlyName()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(GenericTestActivity<string>));

        Assert.Equal("GenericTestActivity<String>", descriptor.Name);
    }

    [Fact]
    public async Task DescribeActivityAsync_NestedActivityInsideGenericType_CreatesDescriptor()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(Container<string>.NestedActivity));

        Assert.Equal("NestedActivity<String>", descriptor.Name);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.NestedActivity<String>", descriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_NestedGenericActivityClosings_HaveDistinctTypeNames()
    {
        var stringDescriptor = await _describer.DescribeActivityAsync(typeof(Container<string>.NestedActivity));
        var intDescriptor = await _describer.DescribeActivityAsync(typeof(Container<int>.NestedActivity));

        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.NestedActivity<String>", stringDescriptor.TypeName);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.NestedActivity<Int32>", intDescriptor.TypeName);
        Assert.NotEqual(stringDescriptor.TypeName, intDescriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_GenericActivity_PreservesName()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(GenericActivity<string>));

        Assert.Equal("GenericActivity<String>", descriptor.Name);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.GenericActivity<String>", descriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_ActivityAttributeType_OverridesGeneratedName()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(AttributedGenericActivity<string>));

        Assert.Equal("ExplicitName", descriptor.Name);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.ExplicitName<String>", descriptor.TypeName);
    }

    private sealed class ActivityWithSensitiveInputs : CodeActivity
    {
        [Input(CanContainSecrets = true)]
        public Input<string?> SensitiveText { get; set; } = new("secret");

        [Input]
        public Input<string?> PublicText { get; set; } = new("public");
    }

    private sealed class Container<T>
    {
        public sealed class NestedActivity : CodeActivity
        {
        }
    }

    private sealed class GenericActivity<T> : CodeActivity
    {
    }

    private sealed class GenericTestActivity<T> : CodeActivity;

    [Activity(Type = "ExplicitName")]
    private sealed class AttributedGenericActivity<T> : CodeActivity;
}
