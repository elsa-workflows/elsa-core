using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Dsl.Interpreters;

/// <inheritdoc />
public partial class WorkflowDefinitionBuilderInterpreter
{
    /// <inheritdoc />
    public override IWorkflowBuilder VisitProgram(ElsaParser.ProgramContext context)
    {
        var stats = context.stat();
        var rootSequence = new Sequence();

        // Push a sequence to allow multiple activities declared in sequence.
        _containerStack.Push(rootSequence);

        // Interpret child nodes.
        VisitChildren(context);

        // Extract activities from child nodes.
        var activities = stats.Select(x => _expressionValue.Get(x) as IActivity).Where(x => x != null).Select(x => x!).ToList();

        if (activities.Count == 1)
        {
            // We only have one activity, so we can use it as the root and discard the root sequence.
            _workflowBuilder.Root = activities.Single();
        }
        else
        {
            // Assign the collected child activities to the root sequence.
            rootSequence.Activities = activities.ToList();
            _workflowBuilder.Root = rootSequence;
        }

        return DefaultResult;
    }
}