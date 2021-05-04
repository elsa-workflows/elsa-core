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
            ICompositeActivityBlueprint? parent,
            string? name,
            string? displayName,
            string? description,
            string type,
            bool persistWorkflow,
            bool loadWorkflowContext,
            bool saveWorkflowContext,
            bool persistOutput,
            string? source) : base(id, parent, name, displayName, description, type, persistWorkflow, loadWorkflowContext, saveWorkflowContext, persistOutput, source)
        {
        }

        public ICollection<IActivityBlueprint> Activities { get; set; } = default!;
        public ICollection<IConnection> Connections { get; set; } = default!;
        public IActivityPropertyProviders ActivityPropertyProviders { get; set; } = default!;
    }
}