using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Management.Services;
using NSubstitute;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class ExpressionDescriptorRegistryTests
{
    [Test]
    public async Task Should_Add_Descriptors_Via_Providers()
    {
        // Arrange
        var descriptor1 = CreateDescriptor("Type1");
        var descriptor2 = CreateDescriptor("Type2");
        var provider1 = CreateProvider(descriptor1);
        var provider2 = CreateProvider(descriptor2);

        // Act
        var registry = CreateRegistry(provider1, provider2);

        // Assert
        var allDescriptors = registry.ListAll().ToList();
        await Assert.That(allDescriptors).Contains(d => d.Type == "Type1");
        await Assert.That(allDescriptors).Contains(d => d.Type == "Type2");
        await Assert.That(allDescriptors.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Should_Find_Descriptor_By_Type()
    {
        // Arrange
        var descriptor = CreateDescriptor("TestType");
        var registry = CreateRegistry(CreateProvider(descriptor));

        // Act
        var found = registry.Find("TestType");

        // Assert
        var foundDescriptor = await Assert.That(found).IsNotNull();
        await Assert.That(foundDescriptor.Type).IsEqualTo("TestType");
    }

    [Test]
    public async Task Should_Find_Descriptor_By_Predicate()
    {
        // Arrange
        var descriptor1 = CreateDescriptor("Type1");
        var descriptor2 = CreateDescriptor("Type2");
        var registry = CreateRegistry(CreateProvider(descriptor1, descriptor2));

        // Act
        var found = registry.Find(d => d.Type == "Type2");

        // Assert
        var descriptor = await Assert.That(found).IsNotNull();
        await Assert.That(descriptor.Type).IsEqualTo("Type2");
    }

    [Test]
    [Arguments("NonExistentType")]
    [Arguments("")]
    [Arguments("WrongType")]
    public async Task Should_Return_Null_When_Descriptor_Not_Found_By_Type(string searchType)
    {
        // Arrange
        var descriptor = CreateDescriptor("ExistingType");
        var registry = CreateRegistry(CreateProvider(descriptor));

        // Act
        var found = registry.Find(searchType);

        // Assert
        await Assert.That(found).IsNull();
    }

    [Test]
    public async Task Should_Return_Null_When_Descriptor_Not_Found_By_Predicate()
    {
        // Arrange
        var descriptor = CreateDescriptor("Type1");
        var registry = CreateRegistry(CreateProvider(descriptor));

        // Act
        var found = registry.Find(d => d.Type == "NonExistent");

        // Assert
        await Assert.That(found).IsNull();
    }

    [Test]
    public async Task Should_List_All_Registered_Descriptors()
    {
        // Arrange
        var descriptor1 = CreateDescriptor("Type1");
        var descriptor2 = CreateDescriptor("Type2");
        var descriptor3 = CreateDescriptor("Type3");
        var registry = CreateRegistry(CreateProvider(descriptor1, descriptor2, descriptor3));

        // Act
        var all = registry.ListAll().ToList();

        // Assert
        await Assert.That(all.Count).IsEqualTo(3);
        await Assert.That(all).Contains(d => d.Type == "Type1");
        await Assert.That(all).Contains(d => d.Type == "Type2");
        await Assert.That(all).Contains(d => d.Type == "Type3");
    }

    [Test]
    public async Task Should_Handle_Duplicate_Registrations_Last_Wins()
    {
        // Arrange - Two providers register the same type
        var handler1 = Substitute.For<IExpressionHandler>();
        var handler2 = Substitute.For<IExpressionHandler>();
        var descriptor1 = CreateDescriptor("DuplicateType", handler1);
        var descriptor2 = CreateDescriptor("DuplicateType", handler2);
        var provider1 = CreateProvider(descriptor1);
        var provider2 = CreateProvider(descriptor2);

        // Act - provider2 is registered after provider1
        var registry = CreateRegistry(provider1, provider2);

        // Assert - Last registration should win
        var found = registry.Find("DuplicateType");
        var descriptor = await Assert.That(found).IsNotNull();
        await Assert.That(descriptor.HandlerFactory(null!)).IsSameReferenceAs(handler2);
    }

    [Test]
    public async Task Should_Add_Single_Descriptor_Via_Add_Method()
    {
        // Arrange
        var registry = CreateRegistry(CreateProvider());
        var newDescriptor = CreateDescriptor("NewType");

        // Act
        registry.Add(newDescriptor);

        // Assert
        var found = registry.Find("NewType");
        var descriptor = await Assert.That(found).IsNotNull();
        await Assert.That(descriptor.Type).IsEqualTo("NewType");
    }

    [Test]
    public async Task Should_Add_Multiple_Descriptors_Via_AddRange_Method()
    {
        // Arrange
        var registry = CreateRegistry(CreateProvider());
        var descriptors = new[]
        {
            CreateDescriptor("Range1"),
            CreateDescriptor("Range2"),
            CreateDescriptor("Range3")
        };

        // Act
        registry.AddRange(descriptors);

        // Assert
        await Assert.That(registry.Find("Range1")).IsNotNull();
        await Assert.That(registry.Find("Range2")).IsNotNull();
        await Assert.That(registry.Find("Range3")).IsNotNull();
    }

    [Test]
    [Arguments(true)]  // Empty provider list
    [Arguments(false)] // Provider with empty descriptors
    public async Task Should_Handle_Empty_Scenarios(bool emptyProviderList)
    {
        // Arrange & Act
        var registry = emptyProviderList
            ? CreateRegistry()
            : CreateRegistry(CreateProvider());

        // Assert
        var all = registry.ListAll().ToList();
        await Assert.That(all).IsEmpty();
    }

    [Test]
    public async Task Should_Enumerate_Descriptors_Multiple_Times()
    {
        // Arrange
        var descriptor = CreateDescriptor("TestType");
        var registry = CreateRegistry(CreateProvider(descriptor));

        // Act
        var first = registry.ListAll().ToList();
        var second = registry.ListAll().ToList();

        // Assert
        await Assert.That(second.Count).IsEqualTo(first.Count);
        await Assert.That(first).HasSingleItem();
        await Assert.That(second).HasSingleItem();
    }
    
    private static ExpressionDescriptor CreateDescriptor(string type)
    {
        var handler = Substitute.For<IExpressionHandler>();
        return CreateDescriptor(type, handler);
    }

    private static ExpressionDescriptor CreateDescriptor(string type, IExpressionHandler handler)
    {
        return new()
        {
            Type = type,
            HandlerFactory = _ => handler
        };
    }

    private static IExpressionDescriptorProvider CreateProvider(params ExpressionDescriptor[] descriptors)
    {
        var provider = Substitute.For<IExpressionDescriptorProvider>();
        provider.GetDescriptors().Returns(descriptors);
        return provider;
    }

    private static ExpressionDescriptorRegistry CreateRegistry(params IExpressionDescriptorProvider[] providers)
    {
        return new(providers);
    }
}
