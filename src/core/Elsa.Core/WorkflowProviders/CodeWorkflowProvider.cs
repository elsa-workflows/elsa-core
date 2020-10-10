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
        private readonly IEnumerable<IWorkflow> _workflows;
        private readonly Func<IWorkflowBuilder> _workflowBuilder;

        public CodeWorkflowProvider(IEnumerable<IWorkflow> workflows, Func<IWorkflowBuilder> workflowBuilder)
        {
            _workflows = workflows;
            _workflowBuilder = workflowBuilder;
        }

        public Task<IEnumerable<Workflow>> GetWorkflowsAsync(CancellationToken cancellationToken) => Task.FromResult(GetWorkflows());
        private IEnumerable<Workflow> GetWorkflows() => from workflow in _workflows let builder = _workflowBuilder() select builder.Build(workflow);
    }
}