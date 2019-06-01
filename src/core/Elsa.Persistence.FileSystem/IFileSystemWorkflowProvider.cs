using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence.FileSystem
{
    public interface IFileSystemWorkflowProvider
    {
        Task SaveAsync(string directory, Workflow value, CancellationToken cancellationToken);
        Task<IEnumerable<Workflow>> ListAsync(string directory, CancellationToken cancellationToken);
    }
}