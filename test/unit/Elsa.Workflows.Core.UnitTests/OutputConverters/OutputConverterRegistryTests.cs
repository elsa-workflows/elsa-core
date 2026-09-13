using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class OutputConverterRegistryTests
{
    [Test]
    public async Task Find_ReturnsDescriptorAndRegistrationForExactId()
    {
        // Arrange
        var registration = CreateRegistration("sample.to-text", typeof(object), typeof(string));
        var sut = new OutputConverterRegistry([registration]);

        // Act
        var descriptor = sut.Find("sample.to-text");
        var resolvedRegistration = sut.FindRegistration("sample.to-text");

        // Assert
        await Assert.That(descriptor).IsSameReferenceAs(registration.Descriptor);
        await Assert.That(resolvedRegistration).IsSameReferenceAs(registration);
    }

    [Test]
    public async Task Find_UsesOrdinalCaseSensitiveIdentity()
    {
        // Arrange
        var registration = CreateRegistration("sample.to-text", typeof(object), typeof(string));
        var sut = new OutputConverterRegistry([registration]);

        // Act
        var descriptor = sut.Find("Sample.To-Text");
        var resolvedRegistration = sut.FindRegistration("Sample.To-Text");

        // Assert
        await Assert.That(descriptor).IsNull();
        await Assert.That(resolvedRegistration).IsNull();
    }

    [Test]
    [Arguments("sample.to-text")]
    [Arguments("SAMPLE.TO-TEXT")]
    public void Constructor_RejectsExactAndCaseOnlyDuplicateIds(string duplicateId)
    {
        // Arrange
        var registrations = new[]
        {
            CreateRegistration("sample.to-text", typeof(object), typeof(string)),
            CreateRegistration(duplicateId, typeof(object), typeof(string))
        };

        // Act
        Action act = () => _ = new OutputConverterRegistry(registrations);

        // Assert
        Assert.ThrowsExactly<InvalidOperationException>(act);
    }

    [Test]
    [Arguments("different.converter")]
    [Arguments("SAMPLE.TO-TEXT")]
    public void Constructor_RejectsServiceKeyThatDoesNotExactlyMatchDescriptorId(string serviceKey)
    {
        // Arrange
        var descriptor = CreateDescriptor("sample.to-text", typeof(object), typeof(string));
        var registration = new OutputConverterRegistration(descriptor, serviceKey, ServiceLifetime.Scoped);

        // Act
        Action act = () => _ = new OutputConverterRegistry([registration]);

        // Assert
        Assert.ThrowsExactly<InvalidOperationException>(act);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public void Constructor_RejectsOpenGenericSourceOrResultTypes(bool useOpenGenericSourceType)
    {
        // Arrange
        var sourceType = useOpenGenericSourceType ? typeof(IEnumerable<>) : typeof(object);
        var resultType = useOpenGenericSourceType ? typeof(string) : typeof(IEnumerable<>);
        var registration = CreateRegistration("sample.open-generic", sourceType, resultType);

        // Act
        Action act = () => _ = new OutputConverterRegistry([registration]);

        // Assert
        Assert.ThrowsExactly<InvalidOperationException>(act);
    }

    [Test]
    public async Task ListAll_ReturnsEveryRegisteredDescriptor()
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
        await Assert.That(ids).IsEquivalentTo(
            ["sample.first", "sample.second"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task FindCompatible_AllowsExactBaseAndInterfaceSourceTypesAndAssignableResultTypes()
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
        await Assert.That(ids).IsEquivalentTo(
            ["source.base", "source.exact", "source.interface"],
            TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task FindCompatible_AcceptsObjectAsADeclaredDestination()
    {
        // Arrange
        var registration = CreateRegistration("sample.to-text", typeof(SourceBase), typeof(string));
        var sut = new OutputConverterRegistry([registration]);

        // Act
        var descriptors = sut.FindCompatible(typeof(DerivedSource), typeof(object)).ToArray();

        // Assert
        await Assert.That(descriptors).Count().IsEqualTo(1);
        await Assert.That(descriptors[0]).IsSameReferenceAs(registration.Descriptor);
    }

    [Test]
    public async Task FindCompatible_DoesNotReverseSourceOrResultAssignability()
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
        await Assert.That(sourceMatches).IsEmpty();
        await Assert.That(resultMatches).IsEmpty();
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
