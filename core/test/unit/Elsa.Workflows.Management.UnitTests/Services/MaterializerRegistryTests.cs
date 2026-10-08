using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Services;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class MaterializerRegistryTests
{
    [Fact]
    public void GetMaterializers_Should_Return_All_Materializers()
    {
        // Arrange
        var materializer1 = CreateMaterializer("materializer1");
        var materializer2 = CreateMaterializer("materializer2");
        var registry = CreateRegistry(materializer1, materializer2);

        // Act
        var result = registry.GetMaterializers().ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.Name == "materializer1");
        Assert.Contains(result, m => m.Name == "materializer2");
    }

    [Fact]
    public void GetMaterializer_Should_Return_Materializer_By_Name()
    {
        // Arrange
        var materializer1 = CreateMaterializer("test-materializer");
        var materializer2 = CreateMaterializer("other-materializer");
        var registry = CreateRegistry(materializer1, materializer2);

        // Act
        var result = registry.GetMaterializer("test-materializer");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-materializer", result.Name);
    }

    [Theory]
    [InlineData("non-existent")]
    [InlineData("")]
    [InlineData("wrong-name")]
    public void GetMaterializer_Should_Return_Null_When_Not_Found(string name)
    {
        // Arrange
        var materializer = CreateMaterializer("existing-materializer");
        var registry = CreateRegistry(materializer);

        // Act
        var result = registry.GetMaterializer(name);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsMaterializerAvailable_Should_Return_True_When_Materializer_Exists()
    {
        // Arrange
        var materializer = CreateMaterializer("available-materializer");
        var registry = CreateRegistry(materializer);

        // Act
        var result = registry.IsMaterializerAvailable("available-materializer");

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("not-available")]
    [InlineData("")]
    [InlineData("wrong-name")]
    public void IsMaterializerAvailable_Should_Return_False_When_Materializer_Does_Not_Exist(string name)
    {
        // Arrange
        var materializer = CreateMaterializer("existing-materializer");
        var registry = CreateRegistry(materializer);

        // Act
        var result = registry.IsMaterializerAvailable(name);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Should_Handle_Empty_Materializer_Collection()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var allMaterializers = registry.GetMaterializers().ToList();
        var foundMaterializer = registry.GetMaterializer("any-name");
        var isAvailable = registry.IsMaterializerAvailable("any-name");

        // Assert
        Assert.Empty(allMaterializers);
        Assert.Null(foundMaterializer);
        Assert.False(isAvailable);
    }

    [Fact]
    public void Should_Cache_Materializers_On_First_Access()
    {
        // Arrange
        var callCount = 0;
        IEnumerable<IWorkflowMaterializer> MaterializersFactory()
        {
            callCount++;
            return new[] { CreateMaterializer("test") };
        }

        var registry = new MaterializerRegistry(MaterializersFactory);

        // Act
        var first = registry.GetMaterializers().ToList();
        var second = registry.GetMaterializers().ToList();
        var third = registry.GetMaterializer("test");
        var fourth = registry.IsMaterializerAvailable("test");

        // Assert
        Assert.Single(first);
        Assert.Single(second);
        Assert.NotNull(third);
        Assert.True(fourth);
        Assert.Equal(1, callCount); // Factory should only be called once
    }

    [Fact]
    public void GetMaterializers_Should_Allow_Multiple_Enumerations()
    {
        // Arrange
        var materializer1 = CreateMaterializer("mat1");
        var materializer2 = CreateMaterializer("mat2");
        var registry = CreateRegistry(materializer1, materializer2);

        // Act
        var first = registry.GetMaterializers().ToList();
        var second = registry.GetMaterializers().ToList();

        // Assert
        Assert.Equal(2, first.Count);
        Assert.Equal(2, second.Count);
        Assert.Equal(first.Count, second.Count);
    }

    [Fact]
    public void GetMaterializer_Should_Return_First_When_Multiple_Materializers_With_Same_Name()
    {
        // Arrange - FirstOrDefault returns the first match
        var materializer1 = CreateMaterializer("duplicate-name");
        var materializer2 = CreateMaterializer("duplicate-name");
        var registry = CreateRegistry(materializer1, materializer2);

        // Act
        var result = registry.GetMaterializer("duplicate-name");

        // Assert
        Assert.NotNull(result);
        Assert.Same(materializer1, result); // FirstOrDefault returns the first match
    }

    private static IWorkflowMaterializer CreateMaterializer(string name)
    {
        var materializer = Substitute.For<IWorkflowMaterializer>();
        materializer.Name.Returns(name);
        return materializer;
    }

    private static MaterializerRegistry CreateRegistry(params IWorkflowMaterializer[] materializers)
    {
        return new(() => materializers);
    }
}
