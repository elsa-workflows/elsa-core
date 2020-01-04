using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Extensions
{
    public static class WorkflowRegistryExtensions
    {
        public static Task<Workflow> GetProcess<T>(this IWorkflowRegistry workflowRegistry)
        {
            return workflowRegistry.GetWorkflowAsync(typeof(T).Name, VersionOptions.Latest);            
        }
    }
}