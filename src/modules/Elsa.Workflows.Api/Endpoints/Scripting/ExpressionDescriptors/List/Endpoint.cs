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
    private static readonly IDictionary<string, string> PrivilegedExpressionPermissions = new Dictionary<string, string>
    {
        ["CSharp"] = PermissionNames.ExecuteCSharpExpressions,
        ["Python"] = PermissionNames.ExecutePythonExpressions
    };

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/descriptors/expression-descriptors");
        ConfigurePermissions("read:*", "read:expression-descriptors");
    }

    /// <inheritdoc />
    public override Task HandleAsync(CancellationToken cancellationToken)
    {
        var descriptors = expressionDescriptorRegistry.ListAll().Where(CanListDescriptor).ToList();
        var models = Map(descriptors).ToList();
        var response = new ListResponse<ExpressionDescriptorModel>(models);
        return Send.OkAsync(response, cancellationToken);
    }

    private bool CanListDescriptor(ExpressionDescriptor descriptor)
    {
        if (!PrivilegedExpressionPermissions.TryGetValue(descriptor.Type, out var permission))
            return true;

        return descriptor.IsBrowsable && User.Claims.Any(x => x.Type == PermissionNames.ClaimType && (x.Value == PermissionNames.All || x.Value == permission));
    }

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
