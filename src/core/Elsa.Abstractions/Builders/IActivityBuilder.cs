using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        IDictionary<string, IActivityPropertyValueProvider>? PropertyValueProviders { get; }
        bool PersistWorkflowEnabled { get; set; }
        bool LoadWorkflowContextEnabled { get; set; }
        bool SaveWorkflowContextEnabled { get; set; }
        bool PersistOutputEnabled { get; set; }
        string? Source { get; }
        IActivityBuilder Add<T>(Action<ISetupActivity<T>>? setup = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) where T : class, IActivity;
        IOutcomeBuilder When(string outcome);
        IActivityBuilder Then(IActivityBuilder targetActivity);
        IConnectionBuilder Then(string activityName);
        IActivityBuilder WithId(string? value);
        IActivityBuilder WithName(string? value);
        IActivityBuilder WithDisplayName(string? value);
        IActivityBuilder WithDescription(string? value);
        IActivityBuilder LoadWorkflowContext(bool value = true);
        IActivityBuilder SaveWorkflowContext(bool value = true);
        IActivityBuilder PersistWorkflow(bool value = true);
        IWorkflowBlueprint Build();
    }
}