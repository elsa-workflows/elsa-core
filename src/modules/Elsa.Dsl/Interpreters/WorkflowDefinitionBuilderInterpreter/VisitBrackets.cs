using Elsa.Expressions.Helpers;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    /// <inheritdoc />
    public override IWorkflowBuilder VisitBracketsExpr(ElsaParser.BracketsExprContext context)
    {
        var propertyType = _expressionType.Get(context.Parent);
        var targetElementType = propertyType?.GetGenericArguments().First() ?? typeof(object);
        var contents = context.exprList().expr();

        var items = contents.Select(x =>
        {
            Visit(x);
            var stringContext = x.GetChild<ElsaParser.StringValueExprContext>(0) ?? x;
            return _expressionValue.Get(stringContext);
        }).ToList();

        var stronglyTypedListType = targetElementType;
        var stronglyTypedList = items.ConvertTo(stronglyTypedListType);

        _expressionValue.Put(context, stronglyTypedList);
        return DefaultResult;
    }
}