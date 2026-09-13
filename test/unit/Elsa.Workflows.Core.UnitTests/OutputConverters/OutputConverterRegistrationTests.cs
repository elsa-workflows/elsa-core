using Elsa.Extensions;
using Elsa.Workflows.Core.UnitTests.OutputConverters.Fixtures;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class OutputConverterRegistrationTests
{
    [Test]
    public async Task ScopedRegistration_ResolvesOneConverterPerScope()
    {
        var services = CreateServices(ServiceLifetime.Scoped);
        using var serviceProvider = services.BuildServiceProvider();
        using var firstScope = serviceProvider.CreateScope();
        using var secondScope = serviceProvider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);
        var secondFromSameScope = firstScope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);
        var second = secondScope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);

        await Assert.That(secondFromSameScope).IsSameReferenceAs(first);
        await Assert.That(second).IsNotSameReferenceAs(first);
    }

    [Test]
    public async Task SingletonRegistration_ResolvesTheSameConverterAcrossScopes()
    {
        var services = CreateServices(ServiceLifetime.Singleton);
        using var serviceProvider = services.BuildServiceProvider();
        using var firstScope = serviceProvider.CreateScope();
        using var secondScope = serviceProvider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);
        var second = secondScope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);

        await Assert.That(second).IsSameReferenceAs(first);
    }

    [Test]
    public async Task TransientRegistration_ResolvesANewConverterForEachRequest()
    {
        var services = CreateServices(ServiceLifetime.Transient);
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var first = scope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);
        var second = scope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);

        await Assert.That(second).IsNotSameReferenceAs(first);
    }

    [Test]
    public async Task Registration_ExposesTheDescriptorThroughTheRegistryWithoutRetainingAConverterInstance()
    {
        var services = CreateServices(ServiceLifetime.Scoped);
        using var serviceProvider = services.BuildServiceProvider();

        var registry = serviceProvider.GetRequiredService<IOutputConverterRegistry>();
        var descriptor = registry.Find(Descriptor.Id);

        await Assert.That(descriptor).IsSameReferenceAs(Descriptor);
        await Assert.That(registry.FindRegistration(Descriptor.Id)!.ServiceKey).IsEqualTo(Descriptor.Id);
    }

    private static ServiceCollection CreateServices(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        services.AddOutputConverter<ReferenceOutputConverter>(Descriptor, lifetime);
        return services;
    }

    internal static OutputConverterDescriptor Descriptor { get; } = new(
        "tests.reference-output",
        typeof(string),
        typeof(string),
        "Reference output converter");
}