using Elsa.Activities.ControlFlow;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Scripting.JavaScript;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowDefinitionBuilder VisitIfStat(ElsaParser.IfStatContext context)
    {
        var ifActivity = new If();
        var conditionExpr = context.expr().GetText();

        var javaScriptExpression = new JavaScriptExpression(conditionExpr);
        ifActivity.Condition = new Input<bool>(javaScriptExpression, new JavaScriptExpressionReference(javaScriptExpression));
            
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