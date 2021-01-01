using Elsa.ActivityResults;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class CompositeActivity : Activity
    {
        internal const string Enter = "Enter";
        
        public virtual void Build(ICompositeActivityBuilder activity)
        {
        }

        private bool IsScheduled
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            if (!IsScheduled)
            {
                IsScheduled = true;
                return Outcome(Enter);
            }

            IsScheduled = false;
            return Done();
        }
    }
}