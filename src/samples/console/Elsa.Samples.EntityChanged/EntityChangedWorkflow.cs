using Elsa.Activities.Entity;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;
using Elsa.Activities.Entity.Models;

namespace Elsa.Samples.EntityChanged
{
    /// <summary>
    /// This workflow gets invoked whenever an entity is added to the repository.
    /// It then blocks execution until the entity is deleted.
    /// </summary>
    public class EntityChangedWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .EntityChanged<Entity>(EntityChangedAction.Added)
                .WriteLine(context => $"Entity '{context.GetInput<EntityChangedContext>().EntityId}' was '{context.GetInput<EntityChangedContext>().Action}'")
                .EntityChanged<Entity>(EntityChangedAction.Deleted)
                .WriteLine(context => $"Entity '{context.GetInput<EntityChangedContext>().EntityId}' was '{context.GetInput<EntityChangedContext>().Action}'");
        }
    }
}
