using Elsa.Workflows;

namespace Elsa.Scripting.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowBuilder VisitStringValueExpr(ElsaParser.StringValueExprContext context)
    {
        var value = context.GetText().Trim('\"');
        _expressionValue.Put(context, value);
        return DefaultResult;
    }

    public override IWorkflowBuilder VisitBackTickStringValueExpr(ElsaParser.BackTickStringValueExprContext context)
    {
        var value = context.GetText().Trim('\"');
        _expressionValue.Put(context, value);
        return DefaultResult;
    }
}