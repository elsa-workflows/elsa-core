using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class OutputConverterRegistryTests
{
    [Fact]
    public void Find_ReturnsDescriptorAndRegistrationForExactId()
    {
        // Arrange
        var registration = CreateRegistration("sample.to-text", typeof(object), typeof(string));
        var sut = new OutputConverterRegistry([registration]);

        // Act
        var descriptor = sut.Find("sample.to-text");
        var resolvedRegistration = sut.FindRegistration("sample.to-text");

        // Assert
        Assert.Same(registration.Descriptor, descriptor);
        Assert.Same(registration, resolvedRegistration);
    }

    [Fact]
    public void Find_UsesOrdinalCaseSensitiveIdentity()
    {
        // Arrange
        var registration = CreateRegistration("sample.to-text", typeof(object), typeof(string));
        var sut = new OutputConverterRegistry([registration]);

        // Act
        var descriptor = sut.Find("Sample.To-Text");
        var resolvedRegistration = sut.FindRegistration("Sample.To-Text");

        // Assert
        Assert.Null(descriptor);
        Assert.Null(resolvedRegistration);
    }

    [Theory]
    [InlineData("sample.to-text")]
    [InlineData("SAMPLE.TO-TEXT")]
    public void Constructor_RejectsExactAndCaseOnlyDuplicateIds(string duplicateId)
    {
        // Arrange
        var registrations = new[]
        {
            CreateRegistration("sample.to-text", typeof(object), typeof(string)),
            CreateRegistration(duplicateId, typeof(object), typeof(string))
        };

        // Act
        var act = () => new OutputConverterRegistry(registrations);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Theory]
    [InlineData("different.converter")]
    [InlineData("SAMPLE.TO-TEXT")]
    public void Constructor_RejectsServiceKeyThatDoesNotExactlyMatchDescriptorId(string serviceKey)
    {
        // Arrange
        var descriptor = CreateDescriptor("sample.to-text", typeof(object), typeof(string));
        var registration = new OutputConverterRegistration(descriptor, serviceKey, ServiceLifetime.Scoped);

        // Act
        var act = () => new OutputConverterRegistry([registration]);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_RejectsOpenGenericSourceOrResultTypes(bool useOpenGenericSourceType)
    {
        // Arrange
        var sourceType = useOpenGenericSourceType ? typeof(IEnumerable<>) : typeof(object);
        var resultType = useOpenGenericSourceType ? typeof(string) : typeof(IEnumerable<>);
        var registration = CreateRegistration("sample.open-generic", sourceType, resultType);

        // Act
        var act = () => new OutputConverterRegistry([registration]);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void ListAll_ReturnsEveryRegisteredDescriptor()
    {
        // Arrange
        var registrations = new[]
        {
            CreateRegistration("sample.first", typeof(object), typeof(string)),
            CreateRegistration("sample.second", typeof(string), typeof(int))
        };
        var sut = new OutputConverterRegistry(registrations);

        // Act
        var ids = sut.ListAll().Select(x => x.Id).Order().ToArray();

        // Assert
        Assert.Equal(["sample.first", "sample.second"], ids);
    }

    [Fact]
    public void FindCompatible_AllowsExactBaseAndInterfaceSourceTypesAndAssignableResultTypes()
    {
        // Arrange
        var registrations = new[]
        {
            CreateRegistration("source.exact", typeof(DerivedSource), typeof(ConcreteResult)),
            CreateRegistration("source.base", typeof(SourceBase), typeof(ConcreteResult)),
            CreateRegistration("source.interface", typeof(ISourceContract), typeof(ConcreteResult)),
            CreateRegistration("source.incompatible", typeof(string), typeof(ConcreteResult)),
            CreateRegistration("result.incompatible", typeof(DerivedSource), typeof(object))
        };
        var sut = new OutputConverterRegistry(registrations);

        // Act
        var ids = sut.FindCompatible(typeof(DerivedSource), typeof(IResultContract))
            .Select(x => x.Id)
            .Order()
            .ToArray();

        // Assert
        Assert.Equal(["source.base", "source.exact", "source.interface"], ids);
    }

    [Fact]
    public void FindCompatible_AcceptsObjectAsADeclaredDestination()
    {
        // Arrange
        var registration = CreateRegistration("sample.to-text", typeof(SourceBase), typeof(string));
        var sut = new OutputConverterRegistry([registration]);

        // Act
        var descriptors = sut.FindCompatible(typeof(DerivedSource), typeof(object));

        // Assert
        Assert.Collection(descriptors, descriptor => Assert.Same(registration.Descriptor, descriptor));
    }

    [Fact]
    public void FindCompatible_DoesNotReverseSourceOrResultAssignability()
    {
        // Arrange
        var narrowedSource = CreateRegistration("source.narrowed", typeof(DerivedSource), typeof(ConcreteResult));
        var widenedResult = CreateRegistration("result.widened", typeof(SourceBase), typeof(IResultContract));

        // Act
        var sourceMatches = new OutputConverterRegistry([narrowedSource])
            .FindCompatible(typeof(SourceBase), typeof(IResultContract));
        var resultMatches = new OutputConverterRegistry([widenedResult])
            .FindCompatible(typeof(DerivedSource), typeof(ConcreteResult));

        // Assert
        Assert.Empty(sourceMatches);
        Assert.Empty(resultMatches);
    }

    private static OutputConverterRegistration CreateRegistration(string id, Type sourceType, Type resultType)
    {
        var descriptor = CreateDescriptor(id, sourceType, resultType);
        return new(descriptor, id, ServiceLifetime.Scoped);
    }

    private static OutputConverterDescriptor CreateDescriptor(string id, Type sourceType, Type resultType) =>
        new(id, sourceType, resultType, id);

    private interface ISourceContract
    {
    }

    private class SourceBase
    {
    }

    private sealed class DerivedSource : SourceBase, ISourceContract
    {
    }

    private interface IResultContract
    {
    }

    private sealed class ConcreteResult : IResultContract
    {
    }
}
