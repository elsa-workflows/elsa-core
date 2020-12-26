using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.WorkflowProviders
{
    /// <summary>
    /// Provides programmatic workflows (.NET types).
    /// </summary>
    public class ProgrammaticWorkflowProvider : WorkflowProvider
    {
        private readonly IEnumerable<IWorkflow> _workflows;
        private readonly Func<IWorkflowBuilder> _workflowBuilder;

        public ProgrammaticWorkflowProvider(ElsaOptions elsaOptions, IServiceProvider serviceProvider, Func<IWorkflowBuilder> workflowBuilder)
        {
            _workflows = elsaOptions.WorkflowFactory.CreateServices(serviceProvider);
            _workflowBuilder = workflowBuilder;
        }

        protected override ValueTask<IEnumerable<IWorkflowBlueprint>> OnGetWorkflowsAsync(CancellationToken cancellationToken) => new(GetWorkflows());
        private IEnumerable<IWorkflowBlueprint> GetWorkflows() => from workflow in _workflows let builder = _workflowBuilder() select builder.Build(workflow);
    }
}