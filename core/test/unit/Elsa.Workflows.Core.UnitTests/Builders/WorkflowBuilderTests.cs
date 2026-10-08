using Elsa.Workflows.Builders;
using Elsa.Workflows.Memory;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Builders;

public class WorkflowBuilderTests
{
    [Fact]
    public void WithVariable_NameAndValue_UsesWorkflowInstanceStorage()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var variable = builder.WithVariable("message", "hello");

        // Assert
        Assert.Equal("message", variable.Name);
        Assert.Equal("hello", variable.Value);
        Assert.Equal(typeof(WorkflowInstanceStorageDriver), variable.StorageDriverType);
        Assert.Contains(variable, builder.Variables);
    }

    [Fact]
    public void WithVariable_NameAndValue_UsesSameStorageAsParameterlessOverload()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
#pragma warning disable CS0618 // Parameterless overload is obsolete but remains the persistence baseline.
        var unnamed = builder.WithVariable<string>();
#pragma warning restore CS0618
        var named = builder.WithVariable("message", "hello");

        // Assert
        Assert.Equal(typeof(WorkflowInstanceStorageDriver), unnamed.StorageDriverType);
        Assert.Equal(unnamed.StorageDriverType, named.StorageDriverType);
    }

    [Fact]
    public void WithVariable_ExistingVariable_DoesNotOverrideStorageDriver()
    {
        // Arrange
        var builder = CreateBuilder();
        var variable = new Variable<string>("message", "hello");

        // Act
        builder.WithVariable(variable);

        // Assert
        Assert.Null(variable.StorageDriverType);
        Assert.Contains(variable, builder.Variables);
    }

    private static WorkflowBuilder CreateBuilder() =>
        new(
            Substitute.For<IActivityVisitor>(),
            Substitute.For<IIdentityGraphService>(),
            Substitute.For<IActivityRegistry>());
}
