using Antlr4.Runtime.Tree;
using Elsa.Workflows;

namespace Elsa.Expressions.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowBuilder VisitArgs(ElsaParser.ArgsContext context)
    {
        var args = context.arg();
            
        var argValues = args.Select(x =>
        {
            Visit(x);
            var childContext = x.expr() ?? (IParseTree)x.expressionMarker();
            return _expressionValue.Get(childContext);
        }).ToList();

        _argValues.Put(context, argValues);

        return DefaultResult;
    }
}