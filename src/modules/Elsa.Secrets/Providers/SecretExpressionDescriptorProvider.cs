using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Secrets.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Providers;

/// <summary>
/// Provides the Secret expression descriptor.
/// </summary>
public class SecretExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    private const string SecretPickerUIHint = "secret-picker";
    private const string SecretPickerEndpoint = "/secrets/picker";

    /// <inheritdoc />
    public IEnumerable<ExpressionDescriptor> GetDescriptors()
    {
        yield return new()
        {
            Type = SecretExpression.TypeName,
            DisplayName = "Secret",
            HandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<SecretExpressionHandler>,
            Properties = new Dictionary<string, object>
            {
                ["UIHint"] = SecretPickerUIHint,
                ["PickerEndpoint"] = SecretPickerEndpoint
            },
            Deserialize = Deserialize
        };
    }

    private static Expression Deserialize(ExpressionSerializationContext context)
    {
        var valueElement = context.JsonElement.TryGetProperty("value", out var v) ? v : default;

        if (valueElement.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return new Expression(SecretExpression.TypeName, null);

        var reference = valueElement.Deserialize<SecretReference>(context.Options);
        return new Expression(SecretExpression.TypeName, reference);
    }
}
