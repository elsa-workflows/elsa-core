using Elsa.Contracts;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowDefinitionBuilder VisitExpressionStat(ElsaParser.ExpressionStatContext context)
    {
        VisitChildren(context);
        var expressionValue = _expressionValue.Get(context.expr());
        _expressionValue.Put(context, expressionValue);
        return DefaultResult;
    }

    public override IWorkflowDefinitionBuilder VisitFunctionExpr(ElsaParser.FunctionExprContext context)
    {
        VisitChildren(context);
        var expressionValue = _expressionValue.Get(context.funcCall());
        _expressionValue.Put(context, expressionValue);
        return DefaultResult;
    }

    public override IWorkflowDefinitionBuilder VisitFuncCall(ElsaParser.FuncCallContext context)
    {
        VisitChildren(context);
            
        var functionName = context.ID().GetText();
        var argsNode = context.args();
        var args = argsNode != null ? _argValues.Get(context.args()) : default;
        var activity = _functionActivityRegistry.ResolveFunction(functionName, args);
            
        _expressionValue.Put(context, activity);
        return DefaultResult;
    }
}