using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Extensions
{
    public static class WorkflowRegistryExtensions
    {
        public static Task<WorkflowDefinitionVersion> GetWorkflowDefinitionAsync<T>(this IWorkflowRegistry workflowRegistry)
        {
            return workflowRegistry.GetWorkflowDefinitionAsync(typeof(T).Name, VersionOptions.Latest);            
        }
    }
}