using Elsa.Api.Client.Abstractions;
using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.ActivityProviders;

/// <summary>
/// Constructs a <see cref="Flowchart"/> activity.
/// </summary>
public class FlowSwitchTypeResolver : ActivityTypeResolverBase
{
    /// <inheritdoc />
    protected override bool GetSupportsType(ActivityTypeResolverContext context) => context.ActivityTypeName == "Elsa.FlowSwitch";

    /// <inheritdoc />
    protected override Type ResolveType(ActivityTypeResolverContext context) => typeof(FlowSwitch);
}