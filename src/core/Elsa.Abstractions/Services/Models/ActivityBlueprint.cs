using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services.Models
{
    public class ActivityBlueprint : IActivityBlueprint
    {
        public ActivityBlueprint()
        {
        }

        public ActivityBlueprint(
            string id,
            string? name,
            string type,
            bool persistWorkflow,
            Func<ActivityExecutionContext, CancellationToken, ValueTask<IActivity>> createActivity)
        {
            Id = id;
            Name = name;
            Type = type;
            PersistWorkflow = persistWorkflow;
            CreateActivityAsync = createActivity;
        }

        public string Id { get; set; } = default!;
        public string? Name { get; set; }
        public string Type { get; set; } = default!;
        public bool PersistWorkflow { get; set; }
        public IWorkflowBlueprint? ChildWorkflow { get; set; }
        
        public Func<ActivityExecutionContext, CancellationToken, ValueTask<IActivity>> CreateActivityAsync { get; set; } = default!;
    }
}