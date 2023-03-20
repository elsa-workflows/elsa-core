using Elsa.JavaScript.Expressions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowBuilder VisitIfStat(ElsaParser.IfStatContext context)
    {
        var ifActivity = new If();
        var conditionExpr = context.expr().GetText();

        var javaScriptExpression = new JavaScriptExpression(conditionExpr);
        ifActivity.Condition = new Input<bool>(javaScriptExpression, new JavaScriptExpressionBlockReference(javaScriptExpression));
            
        var thenStat = context.thenStat().stat();
        var elseStat = context.elseStat()?.stat();
            
        Visit(thenStat);

        var thenActivity = _expressionValue.Get(thenStat);
        ifActivity.Then = (IActivity?)thenActivity;

        if (elseStat != null)
        {
            Visit(elseStat);
            var elseActivity = _expressionValue.Get(elseStat);
            ifActivity.Else = (IActivity?)elseActivity;
        }
            
        _expressionValue.Put(context, ifActivity);
        return DefaultResult;
    }
}