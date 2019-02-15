using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Activities.Http.Services
{
    public interface IHttpWorkflowCache
    {
        Task AddWorkflowAsync(Uri requestPath, Workflow workflow, CancellationToken cancellationToken);
        Task RemoveWorkflowAsync(Uri requestPath, Workflow workflow, CancellationToken cancellationToken);
        Task<IEnumerable<Workflow>> GetWorkflowsByPathAsync(Uri requestPath, CancellationToken cancellationToken);
    }
}