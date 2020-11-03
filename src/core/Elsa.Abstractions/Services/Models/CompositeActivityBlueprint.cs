using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services.Models
{
    public class CompositeActivityBlueprint : ActivityBlueprint, ICompositeActivityBlueprint
    {
        public CompositeActivityBlueprint()
        {
            Activities = new List<IActivityBlueprint>();
            Connections = new List<IConnection>();
            ActivityPropertyProviders = new ActivityPropertyProviders();
        }

        public CompositeActivityBlueprint(
            string id,
            string? name,
            string type,
            bool persistWorkflow,
            bool loadWorkflowContext,
            bool saveWorkflowContext,
            Func<ActivityExecutionContext, CancellationToken, ValueTask<IActivity>> createActivity) : base(id, name, type, persistWorkflow, loadWorkflowContext, saveWorkflowContext, createActivity)
        {
        }

        public ICollection<IActivityBlueprint> Activities { get; set; } = default!;
        public ICollection<IConnection> Connections { get; set; }= default!;
        public IActivityPropertyProviders ActivityPropertyProviders { get; set; } = default!;
    }
}