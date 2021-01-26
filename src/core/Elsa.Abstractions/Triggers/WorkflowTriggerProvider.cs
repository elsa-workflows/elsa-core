using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Triggers
{
    public abstract class WorkflowTriggerProvider<T, TActivity> : IWorkflowTriggerProvider where T : ITrigger where TActivity : IActivity
    {
        public string ForActivityType => typeof(TActivity).Name;
        public virtual ValueTask<IEnumerable<ITrigger>> GetTriggersAsync(TriggerProviderContext<TActivity> context, CancellationToken cancellationToken) => new(GetTriggers(context));
        public virtual IEnumerable<ITrigger> GetTriggers(TriggerProviderContext<TActivity> context) => Enumerable.Empty<ITrigger>();

        async ValueTask<IEnumerable<ITrigger>> IWorkflowTriggerProvider.GetTriggersAsync(TriggerProviderContext context, CancellationToken cancellationToken)
        {
            var supportedType = ForActivityType;
            if (context.ActivityExecutionContext.ActivityBlueprint.Type != supportedType)
                return new ITrigger[0];
            
            return await GetTriggersAsync(new TriggerProviderContext<TActivity>(context.ActivityExecutionContext), cancellationToken);
        }
    }
}