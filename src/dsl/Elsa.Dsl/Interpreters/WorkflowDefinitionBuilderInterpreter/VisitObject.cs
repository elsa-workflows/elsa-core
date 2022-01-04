using System;
using System.Linq;
using Elsa.Contracts;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowDefinitionBuilder VisitObjectExpr(ElsaParser.ObjectExprContext context)
    {
        VisitChildren(context);
        var value = _expressionValue.Get(context.@object());
        _expressionValue.Put(context, value);

        return DefaultResult;
    }

    public override IWorkflowDefinitionBuilder VisitObjectStat(ElsaParser.ObjectStatContext context)
    {
        VisitChildren(context);
        var value = _expressionValue.Get(context.@object());
        _expressionValue.Put(context, value);

        return DefaultResult;
    }

    public override IWorkflowDefinitionBuilder VisitObject(ElsaParser.ObjectContext context)
    {
        var objectTypeName = context.ID().GetText();
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

        _object.Put(context, @object);
        _expressionValue.Put(context, @object);
        VisitChildren(context);

        return DefaultResult;
    }
}