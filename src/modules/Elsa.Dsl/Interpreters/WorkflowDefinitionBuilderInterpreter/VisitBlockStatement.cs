using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter
{
    public override IWorkflowBuilder VisitBlockStat(ElsaParser.BlockStatContext context)
    {
        VisitChildren(context);

        var value = _expressionValue.Get(context.block());
        _expressionValue.Put(context, value);
            
        return DefaultResult;
    }

    public override IWorkflowBuilder VisitBlock(ElsaParser.BlockContext context)
    {
        var sequence = new Sequence();
        _containerStack.Push(sequence);

        var statements = context.stat();

        var values = statements.Select(x =>
        {
            Visit(x);
            return _expressionValue.Get(x);
        }).ToList();

        var activities = values.OfType<IActivity>().ToList();
        sequence.Activities = activities;

        _containerStack.Pop();
        _expressionValue.Put(context, sequence);
            
        return DefaultResult;
    }
}