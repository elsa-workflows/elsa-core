using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;

namespace Elsa.Services
{
    public interface IResumesWorkflows
    {
        Task ResumeWorkflowsAsync(
            IEnumerable<BookmarkFinderResult> results,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default);
    }
}