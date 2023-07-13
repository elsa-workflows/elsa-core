using System.Text.Json;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    /// <inheritdoc />
    public override IWorkflowBuilder VisitObjectExpr(ElsaParser.ObjectExprContext context)
    {
        VisitChildren(context);
        var value = _expressionValue.Get(context.@object());
        _expressionValue.Put(context, value);

        return DefaultResult;
    }

    /// <inheritdoc />
    public override IWorkflowBuilder VisitObjectStat(ElsaParser.ObjectStatContext context)
    {
        VisitChildren(context);
        var value = _expressionValue.Get(context.@object());
        _expressionValue.Put(context, value);

        return DefaultResult;
    }

    /// <inheritdoc />
    public override IWorkflowBuilder VisitObject(ElsaParser.ObjectContext context)
    {
        var @object = GetObject(context);

        _object.Put(context, @object);
        _expressionValue.Put(context, @object);
        VisitChildren(context);

        return DefaultResult;
    }

    private object GetObject(ElsaParser.ObjectContext context)
    {
        var objectTypeName = context.ID().GetText();

        // First, check if the symbol matches an activity type.
        var activityDescriptor = _activityRegistry.Find(x => x.Name == objectTypeName);

        if (activityDescriptor != null)
        {
            // TODO: Refactor this to remove the dependency on JsonElement and JsonSerializerOptions.
            // This limits the ability to use this class in other contexts, such as constructing activities from the DSL.
            var jsonElement = JsonSerializer.Deserialize<JsonElement>("{}");
            var ctorArgs = new ActivityConstructorContext(activityDescriptor, jsonElement, new JsonSerializerOptions());
            return activityDescriptor.Constructor(ctorArgs);
        }

        var objectTypeDescriptor = _typeSystem.ResolveTypeName(objectTypeName);

        if (objectTypeDescriptor == null)
        {
            // Perhaps this is a variable reference?
            if (_definedVariables.TryGetValue(objectTypeName, out var definedVariable))
            {
                _expressionValue.Put(context, definedVariable.Value);
                return DefaultResult;
            }

            // Or a workflow variable?
            var workflowVariableQuery =
                from container in _containerStack
                from variable in container.Variables
                where variable.Name == objectTypeName
                select variable;

            var workflowVariable = workflowVariableQuery.FirstOrDefault();

            if (workflowVariable != null)
            {
                _expressionValue.Put(context, workflowVariable);
                return DefaultResult;
            }

            throw new Exception($"Unknown type: {objectTypeName}");
        }

        var objectType = objectTypeDescriptor.Type;
        var @object = Activator.CreateInstance(objectType)!;

        return @object;
    }
}