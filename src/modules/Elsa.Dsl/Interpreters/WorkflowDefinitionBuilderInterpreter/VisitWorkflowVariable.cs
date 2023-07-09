using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowBuilder VisitVariableDeclarationStat(ElsaParser.VariableDeclarationStatContext context)
    {
        VisitChildren(context);
        var expressionValue = _expressionValue.Get(context.varDecl());
        _expressionValue.Put(context, expressionValue);
        return DefaultResult;
    }

    public override IWorkflowBuilder VisitVarDecl(ElsaParser.VarDeclContext context)
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
            var outputProperty = activityType.GetProperties().FirstOrDefault(x => x.Name == "Result");

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