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
            CancellationToken cancellationToken = default);
    }
}