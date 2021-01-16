using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Builders
{
    public class OutcomeBuilder : IOutcomeBuilder
    {
        public OutcomeBuilder(ICompositeActivityBuilder workflowBuilder, IActivityBuilder source, string outcome = OutcomeNames.Done)
        {
            WorkflowBuilder = workflowBuilder;
            Source = source;
            Outcome = outcome;
        }

        public ICompositeActivityBuilder WorkflowBuilder { get; }
        public IActivityBuilder Source { get; }
        public string Outcome { get; }

        public IActivityBuilder Then<T>(
            Action<ISetupActivity<T>>? setup = default,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) where T : class, IActivity
        {
            var activityBuilder = WorkflowBuilder.Add(setup, null, lineNumber, sourceFile);
            Then(activityBuilder, branch);
            return activityBuilder;
        }

        public IActivityBuilder Then<T>(Action<IActivityBuilder>? branch = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity
        {
            var activityBuilder = WorkflowBuilder.Add<T>(branch, null, lineNumber, sourceFile);
            Then(activityBuilder);
            return activityBuilder;
        }

        public IConnectionBuilder Then(string activityName)
        {
            return WorkflowBuilder.Connect(
                () => Source,
                () => WorkflowBuilder.Activities.First(x => x.Name == activityName),
                Outcome);
        }

        public IConnectionBuilder Then(IActivityBuilder activityBuilder, Action<IActivityBuilder>? branch = default)
        {
            branch?.Invoke(activityBuilder);
            return WorkflowBuilder.Connect(Source, activityBuilder, Outcome);
        }

        public IWorkflowBlueprint Build() => ((IWorkflowBuilder) WorkflowBuilder).BuildBlueprint();
    }
}