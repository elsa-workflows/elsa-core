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

        var reference = DeserializeReference(valueElement, context.Options);
        return new Expression(SecretExpression.TypeName, reference);
    }

    private static SecretReference? DeserializeReference(JsonElement valueElement, JsonSerializerOptions options)
    {
        try
        {
            return valueElement.ValueKind switch
            {
                JsonValueKind.Object => valueElement.Deserialize<SecretReference>(options),
                JsonValueKind.String => DeserializeStringReference(valueElement.GetString(), options),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static SecretReference? DeserializeStringReference(string? value, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmedValue = value.Trim();
        if (trimmedValue.StartsWith('{'))
            return JsonSerializer.Deserialize<SecretReference>(trimmedValue, options);

        return new SecretReference(trimmedValue);
    }
}
