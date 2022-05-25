using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

public class WorkflowIndexingContext
{
    public WorkflowIndexingContext(Workflow workflow, CancellationToken cancellationToken)
    {
        Workflow = workflow;
        CancellationToken = cancellationToken;
    }

    public Workflow Workflow { get; }
    public IDictionary<ITrigger, Register> Registers { get; } = new Dictionary<ITrigger, Register>();
    public CancellationToken CancellationToken { get; }

    public Register GetOrCreateRegister(ITrigger activity)
    {
        if (!Registers.TryGetValue(activity, out var register))
        {
            register = Workflow.CreateRegister();
            Registers[activity] = register;
        }

        return register;
    }
}