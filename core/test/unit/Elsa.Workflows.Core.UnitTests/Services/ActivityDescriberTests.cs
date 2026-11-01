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
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<T>.NestedActivity<String>", descriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_TopLevelAndNestedInsideGenericDeclarer_HaveDistinctTypeNames()
    {
        // Regression: both activities previously generated the same type name,
        // "Elsa.Workflows.Core.UnitTests.Services.NestedActivity<String>", which made them collide in the registry.
        var topLevelDescriptor = await _describer.DescribeActivityAsync(typeof(NestedActivity<string>));
        var nestedDescriptor = await _describer.DescribeActivityAsync(typeof(Container<string>.NestedActivity));

        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.NestedActivity<String>", topLevelDescriptor.TypeName);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<T>.NestedActivity<String>", nestedDescriptor.TypeName);
        Assert.NotEqual(topLevelDescriptor.TypeName, nestedDescriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_NestedActivityWithOwnGenericParameter_PreservesPrefixAndOwnArg()
    {
        // Regression for Container<T>.NestedActivity<U>: the declaring prefix carries the container's real
        // arguments for closed types and the nested activity's own generic argument is preserved.
        var closedDescriptor = await _describer.DescribeActivityAsync(typeof(Container<string>.NestedGenericActivity<int>));
        var openDescriptor = await _describer.DescribeActivityAsync(typeof(Container<>.NestedGenericActivity<>));

        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<String>.NestedGenericActivity<Int32>", closedDescriptor.TypeName);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<T>.NestedGenericActivity<U>", openDescriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_NestedActivityClosingsOverContainer_HaveDistinctTypeNames()
    {
        // Regression: different closings of the declaring type, with the nested activity closed to the same own
        // argument, previously produced the same type name because the declaring prefix rendered the container's
        // open generic definition.
        var stringDescriptor = await _describer.DescribeActivityAsync(typeof(Container<string>.NestedGenericActivity<int>));
        var guidDescriptor = await _describer.DescribeActivityAsync(typeof(Container<Guid>.NestedGenericActivity<int>));

        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<String>.NestedGenericActivity<Int32>", stringDescriptor.TypeName);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<Guid>.NestedGenericActivity<Int32>", guidDescriptor.TypeName);
        Assert.NotEqual(stringDescriptor.TypeName, guidDescriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_NestedGenericActivityClosings_HaveDistinctTypeNames()
    {
        var stringDescriptor = await _describer.DescribeActivityAsync(typeof(Container<string>.NestedActivity));
        var intDescriptor = await _describer.DescribeActivityAsync(typeof(Container<int>.NestedActivity));

        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<T>.NestedActivity<String>", stringDescriptor.TypeName);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<T>.NestedActivity<Int32>", intDescriptor.TypeName);
        Assert.NotEqual(stringDescriptor.TypeName, intDescriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_OpenNestedGenericActivityInsideGenericType_CreatesDescriptor()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(Container<>.NestedActivity));

        Assert.Equal("NestedActivity<T>", descriptor.Name);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<T>.NestedActivity", descriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_GenericActivityInsideNonGenericType_KeepsTypeName()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(PlainContainer.GenericNestedActivity<string>));

        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.GenericNestedActivity<String>", descriptor.TypeName);
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

        // C# forbids a second "NestedActivity<U>" in the same declaring type, so this sibling carries the
        // requested shape: a nested activity with its own generic argument.
        public sealed class NestedGenericActivity<U> : CodeActivity;
    }

    private sealed class NestedActivity<T> : CodeActivity;

    private sealed class PlainContainer
    {
        public sealed class GenericNestedActivity<T> : CodeActivity;
    }

    private sealed class GenericActivity<T> : CodeActivity
    {
    }

    private sealed class GenericTestActivity<T> : CodeActivity;

    [Activity(Type = "ExplicitName")]
    private sealed class AttributedGenericActivity<T> : CodeActivity;
}
