using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.WorkflowProviders
{
    /// <summary>
    /// Provides processes that have been created as workflow classes in C#.
    /// </summary>
    public class CodeWorkflowProvider : IWorkflowProvider
    {
        private readonly IEnumerable<IWorkflow> workflows;
        private readonly Func<WorkflowBuilder> workflowBuilder;

        public CodeWorkflowProvider(IEnumerable<IWorkflow> workflows, Func<WorkflowBuilder> workflowBuilder)
        {
            this.workflows = workflows;
            this.workflowBuilder = workflowBuilder;
        }

        public Task<IEnumerable<Workflow>> GetProcessesAsync(CancellationToken cancellationToken) => Task.FromResult(GetProcesses());
        private IEnumerable<Workflow> GetProcesses() => from process in workflows let builder = workflowBuilder() select builder.Build(process);
    }
}