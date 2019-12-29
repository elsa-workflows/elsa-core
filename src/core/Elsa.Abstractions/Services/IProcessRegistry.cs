using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IProcessRegistry
    {
        Task<Process> GetProcessAsync(
            string id,
            VersionOptions version,
            CancellationToken cancellationToken = default);
    }
}