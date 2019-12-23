using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Extensions
{
    public static class WorkflowRegistryExtensions
    {
        public static Task<WorkflowBlueprint> GetWorkflowDefinitionAsync<T>(this IWorkflowRegistry workflowRegistry)
        {
            return workflowRegistry.GetWorkflowBlueprintAsync(typeof(T).Name, VersionOptions.Latest);            
        }
    }
}