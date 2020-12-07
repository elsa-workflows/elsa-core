using System.Collections.Generic;

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
            string? displayName,
            string type,
            bool persistWorkflow,
            bool loadWorkflowContext,
            bool saveWorkflowContext) : base(id, name, displayName, type, persistWorkflow, loadWorkflowContext, saveWorkflowContext)
        {
        }

        public ICollection<IActivityBlueprint> Activities { get; set; } = default!;
        public ICollection<IConnection> Connections { get; set; } = default!;
        public IActivityPropertyProviders ActivityPropertyProviders { get; set; } = default!;
    }
}