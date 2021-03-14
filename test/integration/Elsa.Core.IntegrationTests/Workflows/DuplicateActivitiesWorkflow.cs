using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class DuplicateActivitiesWorkflow : IWorkflow
    {
        const string duplicateId = "Duplicate";

        public static readonly object Result = new object();

        public virtual void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<SetVariable>(a => a.Set(x => x.VariableName, "Unused").Set(x => x.Value, "Unused").Set(x => x.Id, duplicateId))
                .Then<SetVariable>(a => a.Set(x => x.VariableName, "AlsoUnused").Set(x => x.Value, "Unused").Set(x => x.Id, duplicateId));
        }
    }
}