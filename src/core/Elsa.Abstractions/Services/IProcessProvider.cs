using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    /// <summary>
    /// Represents a source of processes for <see cref="IProcessRegistry"/>
    /// </summary>
    public interface IProcessProvider
    {
        Task<IEnumerable<Process>> GetProcessesAsync(CancellationToken cancellationToken);
    }
}