using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Extensions
{
    public static class WorkflowRegistryExtensions
    {
        public static Task<Process> GetProcess<T>(this IProcessRegistry processRegistry)
        {
            return processRegistry.GetProcessAsync(typeof(T).Name, VersionOptions.Latest);            
        }
    }
}