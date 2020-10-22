using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services.Models
{
    public interface IActivityBlueprint
    {
        public string Id { get; }
        string? Name { get; }
        public string Type { get; }
        public bool PersistWorkflow { get; }
        Func<ActivityExecutionContext, CancellationToken, ValueTask<IActivity>> CreateActivityAsync { get; }
    }
}