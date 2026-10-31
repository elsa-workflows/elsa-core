using Elsa.Extensions;
using Elsa.Workflows.Core.UnitTests.OutputConverters.Fixtures;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters;

public class OutputConverterRegistrationTests
{
    [Fact]
    public void ScopedRegistration_ResolvesOneConverterPerScope()
    {
        var services = CreateServices(ServiceLifetime.Scoped);
        using var serviceProvider = services.BuildServiceProvider();
        using var firstScope = serviceProvider.CreateScope();
        using var secondScope = serviceProvider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);
        var secondFromSameScope = firstScope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);
        var second = secondScope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);

        Assert.Same(first, secondFromSameScope);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void SingletonRegistration_ResolvesTheSameConverterAcrossScopes()
    {
        var services = CreateServices(ServiceLifetime.Singleton);
        using var serviceProvider = services.BuildServiceProvider();
        using var firstScope = serviceProvider.CreateScope();
        using var secondScope = serviceProvider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);
        var second = secondScope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);

        Assert.Same(first, second);
    }

    [Fact]
    public void TransientRegistration_ResolvesANewConverterForEachRequest()
    {
        var services = CreateServices(ServiceLifetime.Transient);
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var first = scope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);
        var second = scope.ServiceProvider.GetRequiredKeyedService<IOutputConverter>(Descriptor.Id);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void Registration_ExposesTheDescriptorThroughTheRegistryWithoutRetainingAConverterInstance()
    {
        var services = CreateServices(ServiceLifetime.Scoped);
        using var serviceProvider = services.BuildServiceProvider();

        var registry = serviceProvider.GetRequiredService<IOutputConverterRegistry>();
        var descriptor = registry.Find(Descriptor.Id);

        Assert.Same(Descriptor, descriptor);
        Assert.Equal(Descriptor.Id, registry.FindRegistration(Descriptor.Id)!.ServiceKey);
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
