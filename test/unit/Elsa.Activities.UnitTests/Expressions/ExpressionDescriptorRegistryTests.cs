using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Management.Services;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Expressions;

public class ExpressionDescriptorRegistryTests
{
    [Fact]
    public void Should_Add_Descriptors_Via_Providers()
    {
        // Arrange
        var descriptor1 = new ExpressionDescriptor { Type = "Type1", HandlerFactory = _ => Substitute.For<IExpressionHandler>() };
        var descriptor2 = new ExpressionDescriptor { Type = "Type2", HandlerFactory = _ => Substitute.For<IExpressionHandler>() };

        var provider1 = Substitute.For<IExpressionDescriptorProvider>();
        provider1.GetDescriptors().Returns(new[] { descriptor1 });

        var provider2 = Substitute.For<IExpressionDescriptorProvider>();
        provider2.GetDescriptors().Returns(new[] { descriptor2 });

        // Act
        var registry = new ExpressionDescriptorRegistry(new[] { provider1, provider2 });

        // Assert
        var allDescriptors = registry.ListAll().ToList();
        Assert.Contains(allDescriptors, d => d.Type == "Type1");
        Assert.Contains(allDescriptors, d => d.Type == "Type2");
        Assert.Equal(2, allDescriptors.Count);
    }

    [Fact]
    public void Should_Find_Descriptor_By_Type()
    {
        // Arrange
        var descriptor = new ExpressionDescriptor { Type = "TestType", HandlerFactory = _ => Substitute.For<IExpressionHandler>() };
        var provider = Substitute.For<IExpressionDescriptorProvider>();
        provider.GetDescriptors().Returns(new[] { descriptor });

        var registry = new ExpressionDescriptorRegistry(new[] { provider });

        // Act
        var found = registry.Find("TestType");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("TestType", found.Type);
    }

    [Fact]
    public void Should_Find_Descriptor_By_Predicate()
    {
        // Arrange
        var descriptor1 = new ExpressionDescriptor { Type = "Type1", HandlerFactory = _ => Substitute.For<IExpressionHandler>() };
        var descriptor2 = new ExpressionDescriptor { Type = "Type2", HandlerFactory = _ => Substitute.For<IExpressionHandler>() };

        var provider = Substitute.For<IExpressionDescriptorProvider>();
        provider.GetDescriptors().Returns(new[] { descriptor1, descriptor2 });

        var registry = new ExpressionDescriptorRegistry(new[] { provider });

        // Act
        var found = registry.Find(d => d.Type == "Type2");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Type2", found.Type);
    }

    [Fact]
    public void Should_Return_Null_When_Descriptor_Not_Found_By_Type()
    {
        // Arrange
        var descriptor = new ExpressionDescriptor { Type = "ExistingType", HandlerFactory = _ => Substitute.For<IExpressionHandler>() };
        var provider = Substitute.For<IExpressionDescriptorProvider>();
        provider.GetDescriptors().Returns(new[] { descriptor });

        var registry = new ExpressionDescriptorRegistry(new[] { provider });

        // Act
        var found = registry.Find("NonExistentType");

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void Should_Return_Null_When_Descriptor_Not_Found_By_Predicate()
    {
        // Arrange
        var descriptor = new ExpressionDescriptor { Type = "Type1", HandlerFactory = _ => Substitute.For<IExpressionHandler>() };
        var provider = Substitute.For<IExpressionDescriptorProvider>();
        provider.GetDescriptors().Returns(new[] { descriptor });

        var registry = new ExpressionDescriptorRegistry(new[] { provider });

        // Act
        var found = registry.Find(d => d.Type == "NonExistent");

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void Should_List_All_Registered_Descriptors()
    {
        // Arrange
        var descriptor1 = new ExpressionDescriptor { Type = "Type1", HandlerFactory = _ => Substitute.For<IExpressionHandler>() };
        var descriptor2 = new ExpressionDescriptor { Type = "Type2", HandlerFactory = _ => Substitute.For<IExpressionHandler>() };
        var descriptor3 = new ExpressionDescriptor { Type = "Type3", HandlerFactory = _ => Substitute.For<IExpressionHandler>() };

        var provider = Substitute.For<IExpressionDescriptorProvider>();
        provider.GetDescriptors().Returns(new[] { descriptor1, descriptor2, descriptor3 });

        var registry = new ExpressionDescriptorRegistry(new[] { provider });

        // Act
        var all = registry.ListAll().ToList();

        // Assert
        Assert.Equal(3, all.Count);
        Assert.Contains(all, d => d.Type == "Type1");
        Assert.Contains(all, d => d.Type == "Type2");
        Assert.Contains(all, d => d.Type == "Type3");
    }

    [Fact]
    public void Should_Handle_Duplicate_Registrations_Last_Wins()
    {
        // Arrange - Two providers register the same type
        var handler1 = Substitute.For<IExpressionHandler>();
        var handler2 = Substitute.For<IExpressionHandler>();

        var descriptor1 = new ExpressionDescriptor { Type = "DuplicateType", HandlerFactory = _ => handler1 };
        var descriptor2 = new ExpressionDescriptor { Type = "DuplicateType", HandlerFactory = _ => handler2 };

        var provider1 = Substitute.For<IExpressionDescriptorProvider>();
        provider1.GetDescriptors().Returns(new[] { descriptor1 });

        var provider2 = Substitute.For<IExpressionDescriptorProvider>();
        provider2.GetDescriptors().Returns(new[] { descriptor2 });

        // Act - provider2 is registered after provider1
        var registry = new ExpressionDescriptorRegistry(new[] { provider1, provider2 });

        // Assert - Last registration should win
        var found = registry.Find("DuplicateType");
        Assert.NotNull(found);
        Assert.Same(handler2, found.HandlerFactory(null!));
    }

    [Fact]
    public void Should_Add_Single_Descriptor_Via_Add_Method()
    {
        // Arrange
        var provider = Substitute.For<IExpressionDescriptorProvider>();
        provider.GetDescriptors().Returns(Array.Empty<ExpressionDescriptor>());

        var registry = new ExpressionDescriptorRegistry(new[] { provider });
        var newDescriptor = new ExpressionDescriptor { Type = "NewType", HandlerFactory = _ => Substitute.For<IExpressionHandler>() };

        // Act
        registry.Add(newDescriptor);

        // Assert
        var found = registry.Find("NewType");
        Assert.NotNull(found);
        Assert.Equal("NewType", found.Type);
    }

    [Fact]
    public void Should_Add_Multiple_Descriptors_Via_AddRange_Method()
    {
        // Arrange
        var provider = Substitute.For<IExpressionDescriptorProvider>();
        provider.GetDescriptors().Returns(Array.Empty<ExpressionDescriptor>());

        var registry = new ExpressionDescriptorRegistry(new[] { provider });
        var descriptors = new[]
        {
            new ExpressionDescriptor { Type = "Range1", HandlerFactory = _ => Substitute.For<IExpressionHandler>() },
            new ExpressionDescriptor { Type = "Range2", HandlerFactory = _ => Substitute.For<IExpressionHandler>() },
            new ExpressionDescriptor { Type = "Range3", HandlerFactory = _ => Substitute.For<IExpressionHandler>() }
        };

        // Act
        registry.AddRange(descriptors);

        // Assert
        Assert.NotNull(registry.Find("Range1"));
        Assert.NotNull(registry.Find("Range2"));
        Assert.NotNull(registry.Find("Range3"));
    }

    [Fact]
    public void Should_Handle_Empty_Provider_List()
    {
        // Arrange & Act
        var registry = new ExpressionDescriptorRegistry(Array.Empty<IExpressionDescriptorProvider>());

        // Assert
        var all = registry.ListAll().ToList();
        Assert.Empty(all);
    }

    [Fact]
    public void Should_Handle_Provider_With_Empty_Descriptors()
    {
        // Arrange
        var provider = Substitute.For<IExpressionDescriptorProvider>();
        provider.GetDescriptors().Returns(Array.Empty<ExpressionDescriptor>());

        // Act
        var registry = new ExpressionDescriptorRegistry(new[] { provider });

        // Assert
        var all = registry.ListAll().ToList();
        Assert.Empty(all);
    }

    [Fact]
    public void Should_Enumerate_Descriptors_Multiple_Times()
    {
        // Arrange
        var descriptor = new ExpressionDescriptor { Type = "TestType", HandlerFactory = _ => Substitute.For<IExpressionHandler>() };
        var provider = Substitute.For<IExpressionDescriptorProvider>();
        provider.GetDescriptors().Returns(new[] { descriptor });

        var registry = new ExpressionDescriptorRegistry(new[] { provider });

        // Act
        var first = registry.ListAll().ToList();
        var second = registry.ListAll().ToList();

        // Assert
        Assert.Equal(first.Count, second.Count);
        Assert.Single(first);
        Assert.Single(second);
    }
}
