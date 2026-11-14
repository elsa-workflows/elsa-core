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
    public async Task DescribeActivityAsync_DescribesCollectionAndDictionaryOfInputs()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(ActivityWithInputAndOutputCollections));

        var items = descriptor.Inputs.Single(x => x.Name == nameof(ActivityWithInputAndOutputCollections.Items));
        var map = descriptor.Inputs.Single(x => x.Name == nameof(ActivityWithInputAndOutputCollections.Map));

        Assert.True(items.IsCollectionOfInput);
        Assert.False(items.IsDictionaryWithValueOfInput);
        Assert.False(items.IsWrapped);
        Assert.Equal(typeof(List<Input<string>>), items.Type);

        Assert.True(map.IsDictionaryWithValueOfInput);
        Assert.False(map.IsCollectionOfInput);
        Assert.False(map.IsWrapped);
        Assert.Equal(typeof(Dictionary<string, Input<object>>), map.Type);
    }

    [Fact]
    public async Task DescribeActivityAsync_DescribesDictionaryOfOutputs()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(ActivityWithInputAndOutputCollections));

        var outputs = descriptor.Outputs.Single(x => x.Name == nameof(ActivityWithInputAndOutputCollections.Outputs));
        var result = descriptor.Outputs.Single(x => x.Name == nameof(ActivityWithInputAndOutputCollections.Result));

        Assert.True(outputs.IsDictionaryWithValueOfOutput);
        Assert.Equal(typeof(Dictionary<string, Output<object>>), outputs.Type);

        Assert.False(result.IsDictionaryWithValueOfOutput);
        Assert.Equal(typeof(string), result.Type);
    }

    [Fact]
    public async Task DescribeActivityAsync_IgnoresUnsupportedCollectionsOfInputsAndOutputs()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(ActivityWithInputAndOutputCollections));

        Assert.DoesNotContain(descriptor.Inputs, x => x.Name == nameof(ActivityWithInputAndOutputCollections.InputsByNumber));
        Assert.DoesNotContain(descriptor.Outputs, x => x.Name == nameof(ActivityWithInputAndOutputCollections.OutputList));
        Assert.DoesNotContain(descriptor.Outputs, x => x.Name == nameof(ActivityWithInputAndOutputCollections.OutputsByNumber));
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
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<String>.NestedActivity", descriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_NestedActivityInsideGenericTypeClosings_HaveDistinctTypeNames()
    {
        var stringDescriptor = await _describer.DescribeActivityAsync(typeof(Container<string>.NestedActivity));
        var intDescriptor = await _describer.DescribeActivityAsync(typeof(Container<int>.NestedActivity));

        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<String>.NestedActivity", stringDescriptor.TypeName);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<Int32>.NestedActivity", intDescriptor.TypeName);
        Assert.NotEqual(stringDescriptor.TypeName, intDescriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_TopLevelGenericAndNestedInsideGenericType_HaveDistinctTypeNames()
    {
        // Regression for #9154: both activities previously collided on
        // "Elsa.Workflows.Core.UnitTests.Services.NestedActivity<String>".
        var topLevelDescriptor = await _describer.DescribeActivityAsync(typeof(NestedActivity<string>));
        var nestedDescriptor = await _describer.DescribeActivityAsync(typeof(Container<string>.NestedActivity));

        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.NestedActivity<String>", topLevelDescriptor.TypeName);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.Container<String>.NestedActivity", nestedDescriptor.TypeName);
        Assert.NotEqual(topLevelDescriptor.TypeName, nestedDescriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_NestedActivityWithOwnGenericParameters_PreservesMainName()
    {
        // Regression: nested activities with generic parameters of their own keep the naming behavior from main,
        // including the arguments inherited from the declaring type and without any declaring type prefix.
        var descriptor = await _describer.DescribeActivityAsync(typeof(Container<string>.NestedGenericActivity<int>));

        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.NestedGenericActivity<String,Int32>", descriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_NestedActivityClosingsOverContainer_HaveDistinctTypeNames()
    {
        var stringDescriptor = await _describer.DescribeActivityAsync(typeof(Container<string>.NestedGenericActivity<int>));
        var guidDescriptor = await _describer.DescribeActivityAsync(typeof(Container<Guid>.NestedGenericActivity<int>));

        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.NestedGenericActivity<String,Int32>", stringDescriptor.TypeName);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.NestedGenericActivity<Guid,Int32>", guidDescriptor.TypeName);
        Assert.NotEqual(stringDescriptor.TypeName, guidDescriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_NestedActivityWithExplicitType_PreservesMainBehavior()
    {
        var descriptor = await _describer.DescribeActivityAsync(typeof(Container<string>.AttributedNestedActivity));

        Assert.Equal("X", descriptor.Name);
        Assert.Equal("Elsa.Workflows.Core.UnitTests.Services.X<String>", descriptor.TypeName);
    }

    [Fact]
    public async Task DescribeActivityAsync_OpenGenericActivities_DoesNotThrow()
    {
        // Open generic definitions are not registered by activity discovery; describing them must simply not throw.
        await _describer.DescribeActivityAsync(typeof(Container<>.NestedActivity));
        await _describer.DescribeActivityAsync(typeof(Container<>.NestedGenericActivity<>));
        await _describer.DescribeActivityAsync(typeof(Container<>.AttributedNestedActivity));
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

    private sealed class ActivityWithInputAndOutputCollections : CodeActivity
    {
        public List<Input<string>> Items { get; set; } = [];
        public Dictionary<string, Input<object>> Map { get; set; } = [];
        public Dictionary<int, Input<string>> InputsByNumber { get; set; } = [];
        public Dictionary<string, Output<object>> Outputs { get; set; } = [];
        public Output<string> Result { get; set; } = new();
        public List<Output<string>> OutputList { get; set; } = [];
        public Dictionary<int, Output<string>> OutputsByNumber { get; set; } = [];
    }

    private sealed class Container<T>
    {
        public sealed class NestedActivity : CodeActivity
        {
        }

        public sealed class NestedGenericActivity<U> : CodeActivity;

        [Activity(Type = "X")]
        public sealed class AttributedNestedActivity : CodeActivity;
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
