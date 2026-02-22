using System.Reflection;
using Elsa.Workflows;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Workflows.Management.Extensions;

/// <summary>
/// <see cref="IServiceCollection"/> extension methods for registering Elsa activity types
/// and variable descriptors via <see cref="ManagementOptions"/>.
/// </summary>
/// <remarks>
/// These extensions are the shell-feature-compatible replacement for calling
/// <c>WorkflowManagementFeature.AddActivitiesFrom&lt;T&gt;()</c> in the old-style feature system.
/// Because <c>services.Configure&lt;ManagementOptions&gt;</c> is additive, multiple features
/// can independently register activities without any coupling to each other.
/// </remarks>
public static class ManagementServiceCollectionExtensions
{
    // -------------------------------------------------------------------------
    // Activities
    // -------------------------------------------------------------------------

    /// <summary>
    /// Registers the supplied activity <paramref name="types"/> with <see cref="ManagementOptions"/>.
    /// </summary>
    public static IServiceCollection AddActivities(this IServiceCollection services, IEnumerable<Type> types) =>
        services.Configure<ManagementOptions>(options =>
        {
            foreach (var type in types)
                options.ActivityTypes.Add(type);
        });

    /// <summary>
    /// Registers a single activity type <typeparamref name="TActivity"/> with <see cref="ManagementOptions"/>.
    /// </summary>
    public static IServiceCollection AddActivity<TActivity>(this IServiceCollection services)
        where TActivity : IActivity =>
        services.AddActivities([typeof(TActivity)]);

    /// <summary>
    /// Scans <paramref name="assembly"/> and registers every concrete, non-generic
    /// <see cref="IActivity"/> implementation found.
    /// </summary>
    public static IServiceCollection AddActivitiesFrom(this IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetExportedTypes()
            .Where(t => typeof(IActivity).IsAssignableFrom(t)
                        && t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });
        return services.AddActivities(types);
    }

    /// <summary>
    /// Scans the assembly that contains <typeparamref name="TMarker"/> and registers every
    /// concrete, non-generic <see cref="IActivity"/> implementation found.
    /// </summary>
    public static IServiceCollection AddActivitiesFrom<TMarker>(this IServiceCollection services) =>
        services.AddActivitiesFrom(typeof(TMarker).Assembly);

    // -------------------------------------------------------------------------
    // Variable descriptors
    // -------------------------------------------------------------------------

    /// <summary>
    /// Registers the supplied <paramref name="descriptors"/> with <see cref="ManagementOptions"/>.
    /// </summary>
    public static IServiceCollection AddVariableDescriptors(
        this IServiceCollection services,
        IEnumerable<VariableDescriptor> descriptors) =>
        services.Configure<ManagementOptions>(options =>
        {
            foreach (var descriptor in descriptors)
                options.VariableDescriptors.Add(descriptor);
        });

    /// <summary>
    /// Registers a single variable descriptor.
    /// </summary>
    public static IServiceCollection AddVariableDescriptor(
        this IServiceCollection services,
        VariableDescriptor descriptor) =>
        services.AddVariableDescriptors([descriptor]);

    /// <summary>
    /// Registers a variable descriptor for <typeparamref name="T"/> with the given
    /// <paramref name="category"/> and optional <paramref name="description"/>.
    /// </summary>
    public static IServiceCollection AddVariableDescriptor<T>(
        this IServiceCollection services,
        string category,
        string? description = null) =>
        services.AddVariableDescriptor(new VariableDescriptor(typeof(T), category, description));
}

