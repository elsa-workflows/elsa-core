using Elsa.Abstractions;
using Elsa.Expressions.Models;
using Elsa.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.Scripting.ExpressionDescriptors.List;

/// <summary>
/// Returns a TypeScript definition that is used by the Monaco editor to display intellisense for JavaScript expressions.
/// </summary>
[UsedImplicitly]
internal class List : ElsaEndpointWithoutRequest<ListResponse<ExpressionDescriptor>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/descriptors/expression-descriptors");
        ConfigurePermissions("read:*", "read:expression-descriptors");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        
    }
}