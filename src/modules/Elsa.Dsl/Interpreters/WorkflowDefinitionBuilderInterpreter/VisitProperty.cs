using System;
using System.Linq;
using System.Reflection;
using Elsa.Expressions.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
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
        SetPropertyValue(@object, propertyInfo, propertyValue);

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
        var propertyValueType = propertyValue?.GetType();
        var inputType = typeof(Input<>).MakeGenericType(underlyingType);

        if (propertyValueType != null)
        {
            var hasCtorWithSpecifiedType = inputType.GetConstructors().Any(x => x.GetParameters().Any(y => y.ParameterType.IsAssignableFrom(propertyValueType)));

            if (hasCtorWithSpecifiedType)
                return (Input)Activator.CreateInstance(inputType, propertyValue)!;
        }

        return propertyValue is ExternalExpressionReference externalExpressionReference 
            ? (Input)Activator.CreateInstance(inputType, externalExpressionReference.Expression, externalExpressionReference.BlockReference)! 
            : (Input)Activator.CreateInstance(inputType, propertyValue.ConvertTo(underlyingType))!;
    }
}