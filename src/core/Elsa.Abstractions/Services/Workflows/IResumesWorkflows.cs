using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Services
{
    public interface IResumesWorkflows
    {
        Task ResumeWorkflowsAsync(
            IEnumerable<BookmarkFinderResult> results,
            WorkflowInput? input = default,
            CancellationToken cancellationToken = default);
    }
}