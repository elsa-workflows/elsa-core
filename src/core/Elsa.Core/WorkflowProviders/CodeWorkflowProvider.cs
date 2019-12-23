using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.WorkflowProviders
{
    /// <summary>
    /// Provides workflow definitions that have been created as workflow classes in C#.
    /// </summary>
    public class CodeWorkflowProvider : IWorkflowProvider
    {
        private readonly IEnumerable<IWorkflow> workflows;
        private readonly Func<IWorkflowBuilder> workflowBuilder;

        public CodeWorkflowProvider(IEnumerable<IWorkflow> workflows, Func<IWorkflowBuilder> workflowBuilder)
        {
            this.workflows = workflows;
            this.workflowBuilder = workflowBuilder;
        }

        public Task<IEnumerable<WorkflowBlueprint>> GetWorkflowDefinitionsAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(GetWorkflowDefinitions());

        private IEnumerable<WorkflowBlueprint> GetWorkflowDefinitions()
        {
            foreach (var workflow in workflows)
            {
                var builder = workflowBuilder();
                builder.WithId(workflow.GetType().Name);
                workflow.Build(builder);
                yield return builder.Build();
            }
        }
    }
}