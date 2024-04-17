using System.Text.Json;
using Elsa.Expressions;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Expressions;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Providers;

/// <inheritdoc />
public class DefaultExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<ExpressionDescriptor> GetDescriptors()
    {
        yield return CreateLiteralDescriptor();
        yield return CreateObjectDescriptor();
        yield return CreateJsonDescriptor();
        yield return CreateDelegateDescriptor();
        yield return CreateVariableDescriptor();
    }

    private ExpressionDescriptor CreateLiteralDescriptor()
    {
        return CreateDescriptor<LiteralExpressionHandler>(
            "Literal",
            "Literal",
            isBrowsable: false,
            memoryBlockReferenceFactory: () => new Literal(),
            deserialize: context =>
            {
                var elementValue = context.JsonElement.TryGetProperty("value", out var v) ? v : default;

                var value = (object?)(elementValue.ValueKind switch
                {
                    JsonValueKind.String => elementValue.GetString(),
                    JsonValueKind.Number => elementValue.GetDecimal(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Undefined => null,
                    JsonValueKind.Null => null,
                    JsonValueKind.Object => elementValue.GetRawText(),
                    JsonValueKind.Array => elementValue.GetRawText(),
                    _ => v.GetRawText()
                });

                return new Expression("Literal", value);
            });
    }

    private ExpressionDescriptor CreateObjectDescriptor() => CreateDescriptor<ObjectExpressionHandler>("Object", "Object", monacoLanguage: "json", isBrowsable: false);

    [Obsolete("Use Object instead.")]
    private ExpressionDescriptor CreateJsonDescriptor() => CreateDescriptor<ObjectExpressionHandler>("Json", "Json", monacoLanguage: "json", isBrowsable: false);

    private ExpressionDescriptor CreateDelegateDescriptor() => CreateDescriptor<DelegateExpressionHandler>("Delegate", "Delegate", false, false);

    private ExpressionDescriptor CreateVariableDescriptor()
    {
        return CreateDescriptor<VariableExpressionHandler>(
            "Variable",
            "Variable",
            isBrowsable: false,
            memoryBlockReferenceFactory: () => new Variable(),
            deserialize: context =>
            {
                var valueElement = context.JsonElement.TryGetProperty("value", out var v) ? v : default;
                var value = valueElement.Deserialize(context.MemoryBlockType, context.Options);
                return new Expression("Variable", value);
            }
        );
    }

    private static ExpressionDescriptor CreateDescriptor<THandler>(
        string expressionType,
        string displayName,
        bool isSerializable = true,
        bool isBrowsable = true,
        string? monacoLanguage = null,
        Func<MemoryBlockReference>? memoryBlockReferenceFactory = default,
        Func<ExpressionSerializationContext, Expression>? deserialize = default)
        where THandler : IExpressionHandler
    {
        var descriptor = new ExpressionDescriptor
        {
            Type = expressionType,
            DisplayName = displayName,
            IsSerializable = isSerializable,
            IsBrowsable = isBrowsable,
            HandlerFactory = sp => ActivatorUtilities.GetServiceOrCreateInstance<THandler>(sp),
            MemoryBlockReferenceFactory = memoryBlockReferenceFactory ?? (() => new MemoryBlockReference())
        };

        if (deserialize != null) 
            descriptor.Deserialize = deserialize;

        if (monacoLanguage != null)
            descriptor.Properties = new
            {
                MonacoLanguage = monacoLanguage
            }.ToDictionary();

        return descriptor;
    }
}