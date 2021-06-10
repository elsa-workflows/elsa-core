using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Bookmarks;

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