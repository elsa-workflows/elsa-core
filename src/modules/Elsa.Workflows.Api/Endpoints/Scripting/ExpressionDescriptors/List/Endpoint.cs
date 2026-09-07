using Elsa.Authorization;
using System.Collections.Frozen;
using Elsa.Abstractions;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.Scripting.ExpressionDescriptors.List;

/// <summary>
/// Returns a TypeScript definition that is used by the Monaco editor to display intellisense for JavaScript expressions.
/// </summary>
[UsedImplicitly]
internal class List(IExpressionDescriptorRegistry expressionDescriptorRegistry) : ElsaEndpointWithoutRequest<ListResponse<ExpressionDescriptorModel>>
{
    /// <summary>
    /// The expression types the host can switch off, which are omitted entirely rather than listed as
    /// non-browsable so a disabled language does not appear in the editor at all. Every other type is listed
    /// and carries <c>IsBrowsable</c> for the client to act on.
    /// </summary>
    /// <remarks>
    /// This was a map from expression type to a per-author permission. The permission was only ever read to
    /// test membership -- the value went unused even before it was retired -- and the decision has always
    /// been the host switch, surfaced as <c>IsBrowsable</c>. A set says that without implying a check that
    /// does not happen. Per-author script trust was considered and declined in #7975: the host switch is the control.
    /// </remarks>
    private static readonly FrozenSet<string> HostCodeExpressionTypes = new[] { "CSharp", "Python" }.ToFrozenSet(StringComparer.Ordinal);

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/descriptors/expression-descriptors");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.DescriptorsExpressions, CoreVerbs.View);
    }

    /// <inheritdoc />
    public override Task HandleAsync(CancellationToken cancellationToken)
    {
        var descriptors = expressionDescriptorRegistry.ListAll().Where(CanListDescriptor).ToList();
        var models = Map(descriptors).ToList();
        var response = new ListResponse<ExpressionDescriptorModel>(models);
        return Send.OkAsync(response, cancellationToken);
    }

    private static bool CanListDescriptor(ExpressionDescriptor descriptor) =>
        !HostCodeExpressionTypes.Contains(descriptor.Type) || descriptor.IsBrowsable;

    private static IEnumerable<ExpressionDescriptorModel> Map(List<ExpressionDescriptor> descriptors) => descriptors.Select(Map);

    private static ExpressionDescriptorModel Map(ExpressionDescriptor descriptor)
    {
        var properties = descriptor.Properties;
        return new ExpressionDescriptorModel(
            descriptor.Type,
            descriptor.DisplayName,
            descriptor.IsSerializable,
            descriptor.IsBrowsable,
            properties);
    }
}

internal record ExpressionDescriptorModel(string Type, string DisplayName, bool IsSerializable, bool IsBrowsable, IDictionary<string, object> Properties);
