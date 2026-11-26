using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz.Simpl;
using Quartz.Spi;

namespace Elsa.Scheduling.Quartz.Services;

/// <summary>
/// Registers <see cref="TenantAwareJobFactory"/> once per container when the host has not supplied its own
/// <see cref="IJobFactory"/>. Quartz 3.15.1's <c>AddQuartz</c> default
/// (<see cref="MicrosoftDependencyInjectionJobFactory"/> via <c>TryAddSingleton</c>) is treated as replaceable.
/// </summary>
internal static class JobFactoryRegistration
{
    /// <summary>
    /// Replaces Quartz's default factory with <see cref="TenantAwareJobFactory"/> unless a host custom
    /// <see cref="IJobFactory"/> is already registered. No-ops when this factory is already present.
    /// Safe to call from every Quartz feature, in any order relative to <c>AddQuartz</c>.
    /// </summary>
    public static void TryUseTenantAwareJobFactory(IServiceCollection services)
    {
        if (HasHostJobFactory(services))
            return;

        if (services.Any(IsTenantAwareJobFactory))
            return;

        services.Replace(ServiceDescriptor.Singleton(typeof(IJobFactory), typeof(TenantAwareJobFactory)));
    }

    private static bool HasHostJobFactory(IServiceCollection services) =>
        services.Any(descriptor => descriptor.ServiceType == typeof(IJobFactory) && IsHostJobFactory(descriptor));

    private static bool IsTenantAwareJobFactory(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(IJobFactory) && GetImplementationType(descriptor) == typeof(TenantAwareJobFactory);

    /// <summary>
    /// Quartz 3.15.1 registers its default as
    /// <c>TryAddSingleton(typeof(IJobFactory), typeof(MicrosoftDependencyInjectionJobFactory))</c>
    /// (ImplementationType set, no factory delegate). <c>UseJobFactory&lt;T&gt;</c> uses the same shape with
    /// <c>Replace</c>. Anything else is a host custom factory and must be kept.
    /// </summary>
    private static bool IsHostJobFactory(ServiceDescriptor descriptor)
    {
        var implementationType = GetImplementationType(descriptor);
        if (implementationType == typeof(MicrosoftDependencyInjectionJobFactory) || implementationType == typeof(TenantAwareJobFactory))
            return false;

        return implementationType is not null || descriptor.ImplementationFactory is not null || descriptor.ImplementationInstance is not null;
    }

    private static Type? GetImplementationType(ServiceDescriptor descriptor) =>
        descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();
}
