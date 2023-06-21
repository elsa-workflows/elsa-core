using Elsa.Api.Client.Abstractions;
using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;
using JetBrains.Annotations;

namespace Elsa.Api.Client.ActivityProviders;

/// <summary>
/// Provides a default implementation of <see cref="IActivityTypeResolver"/> that always resolves the <see cref="Activity"/> type.
/// </summary>
public class DefaultActivityTypeResolver : ActivityTypeResolverBase
{
    /// <inheritdoc />
    public override double Priority => -1;

    /// <inheritdoc />
    protected override bool GetSupportsType(ActivityTypeResolverContext context) => true;

    /// <inheritdoc />
    protected override Type ResolveType(ActivityTypeResolverContext context) => typeof(Activity);
}