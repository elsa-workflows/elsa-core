using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Abstractions;

/// <summary>
/// Provides a base class for <see cref="IActivityTypeResolver"/> implementations.
/// </summary>
public abstract class ActivityTypeResolverBase : IActivityTypeResolver
{
    /// <inheritdoc />
    public virtual double Priority => 0;
    
    /// <summary>
    /// Returns a value indicating whether this provider supports the specified activity type.
    /// </summary>
    /// <param name="context">The <see cref="ActivityTypeResolverContext"/>.</param>
    /// <returns>A value indicating whether this provider supports the specified activity type.</returns>
    protected virtual bool GetSupportsType(ActivityTypeResolverContext context) => false;

    /// <summary>
    /// Resolves the activity type.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    protected virtual Type ResolveType(ActivityTypeResolverContext context) => typeof(Activity);

    bool IActivityTypeResolver.GetSupportsType(ActivityTypeResolverContext context) => GetSupportsType(context);
    Type IActivityTypeResolver.ResolveType(ActivityTypeResolverContext context) => ResolveType(context);
}