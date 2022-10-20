using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Builders
{
    public class ActivityBuilder : IActivityBuilder
    {
        public ActivityBuilder(
            Type activityType,
            string activityTypeName,
            ICompositeActivityBuilder workflowBuilder,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders,
            IDictionary<string, string>? storageProviders,
            int lineNumber,
            string? sourceFile)
        {
            ActivityType = activityType;
            ActivityTypeName = activityTypeName;
            WorkflowBuilder = workflowBuilder;
            PropertyValueProviders = propertyValueProviders;
            StorageProviders = storageProviders;
            LineNumber = lineNumber;
            SourceFile = sourceFile;
        }

        protected ActivityBuilder()
        {
        }

        public Type ActivityType { get; protected set; } = default!;
        public string ActivityTypeName { get; protected set; } = default!;
        public ICompositeActivityBuilder WorkflowBuilder { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public bool PersistWorkflowEnabled { get; set; }
        public bool LoadWorkflowContextEnabled { get; set; }
        public bool SaveWorkflowContextEnabled { get; set; }
        public IDictionary<string, IActivityPropertyValueProvider>? PropertyValueProviders { get; protected set; }
        public IDictionary<string, string>? StorageProviders { get; }
        public IDictionary<string, string> PropertyStorageProviders { get; set; } = new Dictionary<string, string>();
        public int LineNumber { get; }
        public string? SourceFile { get; }
        public string? Source => SourceFile != null && LineNumber != default ? $"{Path.GetFileName(SourceFile)}:{LineNumber}" : default;

        public virtual IActivityBuilder Add<T>(
            string activityTypeName, 
            Action<ISetupActivity<T>>? setup = default,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity =>
            WorkflowBuilder.Add(activityTypeName, setup, branch, lineNumber, sourceFile);

        public IOutcomeBuilder When(string outcome) => new OutcomeBuilder(WorkflowBuilder, this, outcome);

        public virtual IActivityBuilder Then<T>(
            string activityTypeName,
            Action<ISetupActivity<T>>? setup = null,
            Action<IActivityBuilder>? branch = null,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity =>
            When(OutcomeNames.Done).Then(activityTypeName, setup, branch, lineNumber, sourceFile);

        public virtual IActivityBuilder Then<T>(string activityTypeName, Action<IActivityBuilder>? branch = null, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity =>
            When(OutcomeNames.Done).Then<T>(activityTypeName, branch, lineNumber, sourceFile);

        public virtual IActivityBuilder Then(IActivityBuilder targetActivity)
        {
            WorkflowBuilder.Connect(this, targetActivity);
            return this;
        }

        public virtual IActivityBuilder ThenNamed(string activityName)
        {
            WorkflowBuilder.Connect(
                () => this,
                () =>
                {
                    var target = WorkflowBuilder.Activities.FirstOrDefault(x => x.Name == activityName);

                    if (target == null)
                        throw new Exception($"Cannot connect to the specified activity with name \"{activityName}\" because it does not exist in the workflow. Did you make a typo?");
                    
                    return target;
                });

            return this;
        }

        public IActivityBuilder WithId(string? value)
        {
            ActivityId = value!;
            return this;
        }

        public IActivityBuilder WithName(string? value)
        {
            Name = value;
            return this;
        }

        public IActivityBuilder WithDisplayName(string? value)
        {
            DisplayName = value;
            return this;
        }

        public IActivityBuilder WithDescription(string? value)
        {
            Description = value;
            return this;
        }

        public IActivityBuilder LoadWorkflowContext(bool value)
        {
            LoadWorkflowContextEnabled = value;
            return this;
        }

        public IActivityBuilder SaveWorkflowContext(bool value)
        {
            SaveWorkflowContextEnabled = value;
            return this;
        }

        public IActivityBuilder PersistWorkflow(bool value)
        {
            PersistWorkflowEnabled = value;
            return this;
        }

        public IWorkflowBlueprint Build() => ((IWorkflowBuilder) WorkflowBuilder).BuildBlueprint();
    }
}