using Elsa.Api.Client.Models;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Contracts;

/// <summary>
/// Resolves an activity type.
/// </summary>
public interface IActivityTypeResolver
{
    /// <summary>
    /// Gets the priority of this activity provider. Activity providers with a higher priority are considered first.
    /// </summary>
    double Priority { get; }
    
    /// <summary>
    /// Returns a value indicating whether this activity provider supports the specified activity type.
    /// </summary>
    /// <param name="context">The <see cref="ActivityTypeResolverContext"/>.</param>
    /// <returns>A value indicating whether this activity provider supports the specified activity type.</returns>
    bool GetSupportsType(ActivityTypeResolverContext context);
    
    /// <summary>
    /// Creates an instance of <see cref="Activity"/> for the specified activity type.
    /// </summary>
    /// <param name="context">The <see cref="ActivityTypeResolverContext"/>.</param>
    /// <returns>The resolved type.</returns>
    Type ResolveType(ActivityTypeResolverContext context);
}