using System.Reflection;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    /// <inheritdoc />
    public override IWorkflowBuilder VisitProperty(ElsaParser.PropertyContext context)
    {
        var @object = _object.Get(context.Parent.Parent.Parent);
        var objectType = @object.GetType();
        var propertyName = context.ID().GetText();
        var propertyInfo = objectType.GetProperty(propertyName);

        if (propertyInfo == null)
            throw new Exception($"Type {objectType.Name} does not have a public property named {propertyName}.");

        _expressionType.Put(context, propertyInfo.PropertyType);
        VisitChildren(context);

        var propertyValue = _expressionValue.Get(context.expr());
        var propertyType = propertyInfo.PropertyType;
        var parsedPropertyValue = typeof(Input).IsAssignableFrom(propertyType) ? propertyValue : propertyValue.ConvertTo(propertyType);
        SetPropertyValue(@object, propertyInfo, parsedPropertyValue);

        return DefaultResult;
    }

    private void SetPropertyValue(object target, PropertyInfo propertyInfo, object? value)
    {
        if (typeof(Input).IsAssignableFrom(propertyInfo.PropertyType))
            value = CreateInputValue(propertyInfo, value);

        propertyInfo.SetValue(target, value, null);
    }

    private Input CreateInputValue(PropertyInfo propertyInfo, object? propertyValue)
    {
        var underlyingType = propertyInfo.PropertyType.GetGenericArguments().First();
        var parsedPropertyValue = propertyValue.ConvertTo(underlyingType);
        var propertyValueType = parsedPropertyValue?.GetType();
        var inputType = typeof(Input<>).MakeGenericType(underlyingType);

        if (propertyValueType != null)
        {
            // Create a literal value.
            var literalType = typeof(Literal<>).MakeGenericType(underlyingType);
            var hasCtorWithSpecifiedType = inputType.GetConstructors().Any(x => x.GetParameters().Any(y => y.ParameterType.IsAssignableFrom(literalType)));

            if (hasCtorWithSpecifiedType)
            {
                var literalValue = Activator.CreateInstance(literalType, parsedPropertyValue)!;
                return (Input)Activator.CreateInstance(inputType, literalValue)!;
            }
        }

        return parsedPropertyValue is ExternalExpressionReference externalExpressionReference
            ? (Input)Activator.CreateInstance(inputType, externalExpressionReference.Expression, externalExpressionReference.BlockReference)!
            : (Input)Activator.CreateInstance(inputType, parsedPropertyValue)!;
    }
}