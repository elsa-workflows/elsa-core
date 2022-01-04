using Elsa.Contracts;

namespace Elsa.Models;

public class WorkflowIndexingContext
{
    public WorkflowIndexingContext(Workflow workflow)
    {
        Workflow = workflow;
    }

    public Workflow Workflow { get; }
    public IDictionary<ITrigger, Register> Registers { get; } = new Dictionary<ITrigger, Register>();

    public Register GetOrCreateRegister(ITrigger activity)
    {
        if (!Registers.TryGetValue(activity, out var register))
        {
            register = new Register();
            Registers[activity] = register;
        }

        return register;
    }
}