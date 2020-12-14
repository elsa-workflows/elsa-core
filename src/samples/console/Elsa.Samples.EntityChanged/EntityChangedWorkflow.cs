using Elsa.Activities.Entity;
using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Activities.Entity.Models;

namespace Elsa.Samples.EntityChanged
{
    public class EntityChangedWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .EntityChanged<Entity>(EntityChangedAction.Added)
                .WriteLine(context => $"Entity '{((EntityChangedModel)context.Input)!.EntityName}' was '{((EntityChangedModel)context.Input).Action}'");
        }
    }
}
