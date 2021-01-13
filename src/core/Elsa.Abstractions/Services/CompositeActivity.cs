using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class CompositeActivity : Activity
    {
        internal const string Enter = "Enter";
        
        public virtual void Build(ICompositeActivityBuilder activity)
        {
        }

        public bool IsScheduled
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (!IsScheduled)
            {
                context.WorkflowInstance.Scopes.Push(Id);
                IsScheduled = true;
                await OnEnterAsync(context);
                return Outcome(Enter);
            }

            IsScheduled = false;
            await OnExitAsync(context);

            var finishOutput = context.Input as FinishOutput;
            var outcomes = new List<string> { OutcomeNames.Done };
            var output = default(object?);

            if (finishOutput != null)
            {
                if(!string.IsNullOrWhiteSpace(finishOutput.Outcome))
                    outcomes.Add(finishOutput.Outcome!);

                output = finishOutput.Output;
            }
            
            return Combine(Outcomes(outcomes), Output(output));
        }

        protected virtual ValueTask OnEnterAsync(ActivityExecutionContext context)
        {
            OnEnter(context);
            return new();
        }

        protected virtual ValueTask OnExitAsync(ActivityExecutionContext context)
        {
            OnExit(context);
            return new();
        }

        protected virtual void OnEnter(ActivityExecutionContext context)
        {
        }
        
        protected virtual void OnExit(ActivityExecutionContext context)
        {
        }
    }
}