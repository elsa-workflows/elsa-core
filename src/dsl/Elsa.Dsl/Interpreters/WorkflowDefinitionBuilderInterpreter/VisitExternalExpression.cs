using Elsa.Contracts;
using Elsa.Models;
using Elsa.Scripting.JavaScript;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowDefinitionBuilder VisitExpressionMarker(ElsaParser.ExpressionMarkerContext context)
    {
        var language = context.ID();
        var expressionContent = context.expressionContent().GetText();

        // TODO: Determine actual expression type based on specified language.
        var expression = new JavaScriptExpression(expressionContent);
        var expressionReference = new JavaScriptExpressionReference(expression);
        var externalReference = new ExternalExpressionReference(expression, expressionReference);
        _expressionValue.Put(context, externalReference);

        return DefaultResult;
    }
}

public record ExternalExpressionReference(IExpression Expression, RegisterLocationReference Reference);