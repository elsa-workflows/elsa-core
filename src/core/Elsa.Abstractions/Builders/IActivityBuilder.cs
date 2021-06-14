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
        public string ActivityTypeName { get; }
        string ActivityId { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        IDictionary<string, IActivityPropertyValueProvider>? PropertyValueProviders { get; }
        IDictionary<string,string> PropertyStorageProviders { get; set; }
        bool PersistWorkflowEnabled { get; set; }
        bool LoadWorkflowContextEnabled { get; set; }
        bool SaveWorkflowContextEnabled { get; set; }
        string? Source { get; }
        IActivityBuilder Add<T>(string activityTypeName, Action<ISetupActivity<T>>? setup = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) where T : class, IActivity;
        IOutcomeBuilder When(string outcome);
        IActivityBuilder Then(IActivityBuilder targetActivity);
        IActivityBuilder ThenNamed(string activityName);
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