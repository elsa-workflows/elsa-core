using System;
using System.Collections.Generic;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IActivityBuilder : IBuilder
    {
        IWorkflowBuilder WorkflowBuilder { get; }
        IActivity Activity { get; }
        IDictionary<string, IActivityPropertyValueProvider>? PropertyValueProviders { get; }
        IActivityBuilder Add<T>(Action<ISetupActivity<T>>? setup = default) where T : class, IActivity;
        IOutcomeBuilder When(string outcome);
        IActivityBuilder Then(IActivityBuilder targetActivity);
        IActivity BuildActivity();
        Workflow Build();
    }
}