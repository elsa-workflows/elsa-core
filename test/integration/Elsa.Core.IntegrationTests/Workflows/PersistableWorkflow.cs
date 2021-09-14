using Elsa.Activities.UserTask.Activities;
using Elsa.Builders;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Models;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class PersistableWorkflow : IWorkflow
    {
        public static readonly object Result = new object();

        public virtual void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<SetVariable>(a => a.Set(x => x.VariableName, "Unused").Set(x => x.Value, "Unused"))
                .Then<UserTask>(setup: s => {
                    s.Set(x => x.Actions, c => new [] { "Foo", "Bar" });
                })
                .Finish(x => x.WithOutput(Result));
        }

        public class OnSuspend : PersistableWorkflow
        {
            public override void Build(IWorkflowBuilder builder)
            {
                builder.WithPersistenceBehavior(WorkflowPersistenceBehavior.Suspended);
                base.Build(builder);
            }
        }

        public class OnActivityExecuted : PersistableWorkflow
        {
            public override void Build(IWorkflowBuilder builder)
            {
                builder.WithPersistenceBehavior(WorkflowPersistenceBehavior.ActivityExecuted);
                base.Build(builder);
            }
        }

        public class OnWorkflowBurst : PersistableWorkflow
        {
            public override void Build(IWorkflowBuilder builder)
            {
                builder.WithPersistenceBehavior(WorkflowPersistenceBehavior.WorkflowBurst);
                base.Build(builder);
            }
        }
    }
}