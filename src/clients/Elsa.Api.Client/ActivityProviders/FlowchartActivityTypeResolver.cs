using Elsa.Api.Client.Abstractions;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.ActivityProviders;

/// <summary>
/// Constructs a <see cref="Flowchart"/> activity.
/// </summary>
public class FlowchartActivityTypeResolver : ActivityTypeResolverBase
{
    /// <inheritdoc />
    protected override bool GetSupportsType(ActivityTypeResolverContext context) => context.ActivityTypeName == "Elsa.Flowchart";

    /// <inheritdoc />
    protected override Type ResolveType(ActivityTypeResolverContext context) => typeof(Flowchart);
}