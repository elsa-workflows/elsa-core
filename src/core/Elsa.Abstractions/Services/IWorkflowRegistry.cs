using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowRegistry
    {
        Task<Workflow> GetWorkflowAsync(
            string id,
            VersionOptions version,
            CancellationToken cancellationToken = default);
    }
}