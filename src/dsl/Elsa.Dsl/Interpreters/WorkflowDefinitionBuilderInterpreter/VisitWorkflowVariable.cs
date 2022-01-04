using System;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowDefinitionBuilder VisitVariableDeclarationStat(ElsaParser.VariableDeclarationStatContext context)
    {
        VisitChildren(context);
        var expressionValue = _expressionValue.Get(context.varDecl());
        _expressionValue.Put(context, expressionValue);
        return DefaultResult;
    }

    public override IWorkflowDefinitionBuilder VisitVarDecl(ElsaParser.VarDeclContext context)
    {
        var workflowVariableName = context.ID().GetText();

        VisitChildren(context);

        var initExpr = context.expr();
        var workflowVariableValue = initExpr != null ? _expressionValue.Get(initExpr) : default;
            
        var workflowVariable = new Variable
        {
            Name = workflowVariableName
        };
            
        if (workflowVariableValue is IActivity activity)
        {
            // When an activity is assigned to a workflow variable, what we really are doing is setting the variable to the activity's output.
            var activityType = activity.GetType();
            var outputProperty = activityType.GetProperty("Output");

            if (outputProperty == null)
                throw new Exception("Cannot assign output of an activity that does not have an Output property.");

            var outputValue = Activator.CreateInstance(outputProperty.PropertyType, workflowVariable, default);
            outputProperty.SetValue(activity, outputValue);
                
            _expressionValue.Put(context, activity);
        }

        var currentContainer = _containerStack.Peek();
        currentContainer.Variables.Add(workflowVariable);

        return DefaultResult;
    }
}