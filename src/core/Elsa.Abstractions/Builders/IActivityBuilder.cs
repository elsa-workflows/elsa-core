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
        IWorkflowBuilder WorkflowBuilder { get; }
        public Type ActivityType { get; }
        string? ActivityId { get;  }
        IDictionary<string, IActivityPropertyValueProvider>? PropertyValueProviders { get; }
        IActivityBuilder Add<T>(Action<ISetupActivity<T>>? setup = default) where T : class, IActivity;
        IOutcomeBuilder When(string outcome);
        IActivityBuilder Then(IActivityBuilder targetActivity);
        IActivityBuilder WithId(string id);
        Func<ActivityExecutionContext, CancellationToken, Task<IActivity>> BuildActivityAsync();
        IWorkflowBlueprint Build();
    }
}