using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IActivityBuilder : IBuilder
    {
        ICompositeActivityBuilder WorkflowBuilder { get; set; }
        public Type ActivityType { get; }
        string ActivityId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        IDictionary<string, IActivityPropertyValueProvider>? PropertyValueProviders { get; }
        bool PersistWorkflow { get; set; }
        IActivityBuilder Add<T>(Action<ISetupActivity<T>>? setup = default) where T : class, IActivity;
        IOutcomeBuilder When(string outcome);
        IActivityBuilder Then(IActivityBuilder targetActivity);
        IConnectionBuilder Then(string activityName);
        IActivityBuilder WithId(string? id);
        IActivityBuilder WithName(string? name);
        Func<ActivityExecutionContext, CancellationToken, ValueTask<IActivity>> BuildActivityAsync();
        //IWorkflowBlueprint Build();
    }
}