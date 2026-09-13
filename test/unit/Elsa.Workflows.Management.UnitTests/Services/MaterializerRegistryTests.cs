using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Services;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class MaterializerRegistryTests
{
    [Test]
    public async Task GetMaterializers_Should_Return_All_Materializers()
    {
        // Arrange
        var materializer1 = CreateMaterializer("materializer1");
        var materializer2 = CreateMaterializer("materializer2");
        var registry = CreateRegistry(materializer1, materializer2);

        // Act
        var result = registry.GetMaterializers().ToList();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result).Contains(m => m.Name == "materializer1");
        await Assert.That(result).Contains(m => m.Name == "materializer2");
    }

    [Test]
    public async Task GetMaterializer_Should_Return_Materializer_By_Name()
    {
        // Arrange
        var materializer1 = CreateMaterializer("test-materializer");
        var materializer2 = CreateMaterializer("other-materializer");
        var registry = CreateRegistry(materializer1, materializer2);

        // Act
        var result = registry.GetMaterializer("test-materializer");

        // Assert
        var materializer = await Assert.That(result).IsNotNull();
        await Assert.That(materializer.Name).IsEqualTo("test-materializer");
    }

    [Test]
    [Arguments("non-existent")]
    [Arguments("")]
    [Arguments("wrong-name")]
    public async Task GetMaterializer_Should_Return_Null_When_Not_Found(string name)
    {
        // Arrange
        var materializer = CreateMaterializer("existing-materializer");
        var registry = CreateRegistry(materializer);

        // Act
        var result = registry.GetMaterializer(name);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task IsMaterializerAvailable_Should_Return_True_When_Materializer_Exists()
    {
        // Arrange
        var materializer = CreateMaterializer("available-materializer");
        var registry = CreateRegistry(materializer);

        // Act
        var result = registry.IsMaterializerAvailable("available-materializer");

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    [Arguments("not-available")]
    [Arguments("")]
    [Arguments("wrong-name")]
    public async Task IsMaterializerAvailable_Should_Return_False_When_Materializer_Does_Not_Exist(string name)
    {
        // Arrange
        var materializer = CreateMaterializer("existing-materializer");
        var registry = CreateRegistry(materializer);

        // Act
        var result = registry.IsMaterializerAvailable(name);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Should_Handle_Empty_Materializer_Collection()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var allMaterializers = registry.GetMaterializers().ToList();
        var foundMaterializer = registry.GetMaterializer("any-name");
        var isAvailable = registry.IsMaterializerAvailable("any-name");

        // Assert
        await Assert.That(allMaterializers).IsEmpty();
        await Assert.That(foundMaterializer).IsNull();
        await Assert.That(isAvailable).IsFalse();
    }

    [Test]
    public async Task Should_Cache_Materializers_On_First_Access()
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
        await Assert.That(first).HasSingleItem();
        await Assert.That(second).HasSingleItem();
        await Assert.That(third).IsNotNull();
        await Assert.That(fourth).IsTrue();
        await Assert.That(callCount).IsEqualTo(1); // Factory should only be called once
    }

    [Test]
    public async Task GetMaterializers_Should_Allow_Multiple_Enumerations()
    {
        // Arrange
        var materializer1 = CreateMaterializer("mat1");
        var materializer2 = CreateMaterializer("mat2");
        var registry = CreateRegistry(materializer1, materializer2);

        // Act
        var first = registry.GetMaterializers().ToList();
        var second = registry.GetMaterializers().ToList();

        // Assert
        await Assert.That(first.Count).IsEqualTo(2);
        await Assert.That(second.Count).IsEqualTo(2);
        await Assert.That(second.Count).IsEqualTo(first.Count);
    }

    [Test]
    public async Task GetMaterializer_Should_Return_First_When_Multiple_Materializers_With_Same_Name()
    {
        // Arrange - FirstOrDefault returns the first match
        var materializer1 = CreateMaterializer("duplicate-name");
        var materializer2 = CreateMaterializer("duplicate-name");
        var registry = CreateRegistry(materializer1, materializer2);

        // Act
        var result = registry.GetMaterializer("duplicate-name");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsSameReferenceAs(materializer1); // FirstOrDefault returns the first match
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
