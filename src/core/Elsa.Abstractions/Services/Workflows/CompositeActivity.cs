using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    [Browsable(false)]
    public class CompositeActivity : Activity
    {
        internal const string Enter = "Enter";

        public virtual void Build(ICompositeActivityBuilder builder)
        {
        }

        [ActivityOutput] public object? Output { get; set; }

        public bool IsScheduled
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        public override IDictionary<string, object?> Data
        {
            get
            {
                // When executing a composite activity's workflow, child activities might try and reference the composite activity's "input" properties.
                // Out of the box, that doesn't work, since "this" points to a new instance with an empty Data object.
                // Instead, we need to capture the Data object of the composite activity (the parent of the currently executing child activity) using the ambient activity execution context.
                if (AmbientActivityExecutionContext.Current == null)
                    return base.Data;

                var context = AmbientActivityExecutionContext.Current;

                // Check if the currently executing activity is this composite activity.
                if (context.ActivityBlueprint.Id == Id)
                    return base.Data;

                // A child activity is attempting to retrieve property data from its parent composite activity.
                var parentId = context.ActivityBlueprint.Parent!.Id;
                return context.WorkflowInstance.ActivityData.GetItem(parentId)!;
            }
            set => base.Data = value;
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (!IsScheduled)
            {
                context.WorkflowInstance.Scopes.Push(new ActivityScope(Id));
                IsScheduled = true;
                await OnEnterAsync(context);
                return Outcome(Enter);
            }

            IsScheduled = false;

            var finishOutput = context.Input as FinishOutput;
            var outcomes = new List<string> { OutcomeNames.Done };
            var output = default(object?);

            if (finishOutput != null)
            {
                // If any outcomes were specified explicitly, we use them instead of Done.
                var finishOutcomes = finishOutput.Outcomes.ToList();

                if (finishOutcomes.Any())
                    outcomes = finishOutcomes;

                output = finishOutput.Output;
            }

            await OnExitAsync(context, output, outcomes);

            Output = output;
            context.WorkflowExecutionContext.ClearScheduledActivities(Id);
            await context.WorkflowExecutionContext.RemoveBlockingActivitiesAsync(Id);
            return Outcomes(outcomes);
        }

        protected virtual ValueTask OnEnterAsync(ActivityExecutionContext context)
        {
            OnEnter(context);
            return new();
        }

        protected virtual ValueTask OnExitAsync(ActivityExecutionContext context, object? output, IList<string> outcomes)
        {
            OnExit(context, output, outcomes);
            return new();
        }

        protected virtual void OnEnter(ActivityExecutionContext context)
        {
        }

        protected virtual void OnExit(ActivityExecutionContext context, object? output, IList<string> outcomes)
        {
        }
    }
}