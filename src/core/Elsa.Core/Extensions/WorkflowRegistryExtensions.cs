using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Extensions
{
    public static class WorkflowRegistryExtensions
    {
        public static void RegisterWorkflows(this IWorkflowRegistry registry,
            IEnumerable<WorkflowDefinitionVersion> workflowDefinitions)
        {
            foreach (var workflowDefinition in workflowDefinitions)
            {
                registry.RegisterWorkflow(workflowDefinition);
            }
        }
    }
}