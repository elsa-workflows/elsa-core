using Elsa.Expressions;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Expressions;
using Elsa.Workflows.Core.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Providers;

/// <inheritdoc />
public class DefaultExpressionDescriptorProvider : IExpressionDescriptorProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<ExpressionDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var literal = CreateLiteralDescriptor();
        var @object = CreateObjectDescriptor();
        var json = CreateJsonDescriptor();
        var @delegate = CreateDelegateDescriptor();
        var variable = CreateVariableDescriptor();

        return ValueTask.FromResult<IEnumerable<ExpressionDescriptor>>(new[] { literal, @object, json, @delegate, variable });
    }

    private ExpressionDescriptor CreateLiteralDescriptor() => CreateDescriptor<LiteralExpressionHandler>("Literal", "Literal");
    private ExpressionDescriptor CreateObjectDescriptor() => CreateDescriptor<ObjectExpressionHandler>("Object", "Object");

    [Obsolete("Use Object instead.")]
    private ExpressionDescriptor CreateJsonDescriptor() => CreateDescriptor<ObjectExpressionHandler>("Json", "Json");

    private ExpressionDescriptor CreateDelegateDescriptor() => CreateDescriptor<DelegateExpressionHandler>("Delegate", "Delegate");
    private ExpressionDescriptor CreateVariableDescriptor() => CreateDescriptor<VariableExpressionHandler>("Variable", "Variable", () => new Variable());

    private static ExpressionDescriptor CreateDescriptor<THandler>(string type, string displayName, Func<MemoryBlockReference>? memoryBlockReferenceFactory = default) where THandler : IExpressionHandler =>
        new()
        {
            Type = type,
            DisplayName = displayName,
            HandlerFactory = sp => ActivatorUtilities.GetServiceOrCreateInstance<THandler>(sp),
            MemoryBlockReferenceFactory = memoryBlockReferenceFactory ?? (() => new MemoryBlockReference())
        };
}