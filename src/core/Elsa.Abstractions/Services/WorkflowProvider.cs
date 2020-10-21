using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class WorkflowProvider : IWorkflowProvider
    {
        protected virtual async ValueTask<IEnumerable<IWorkflowBlueprint>> OnGetWorkflowsAsync(CancellationToken cancellationToken) => await GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);
        public virtual IAsyncEnumerable<IWorkflowBlueprint> GetWorkflowsAsync(CancellationToken cancellationToken) => AsyncEnumerable.Empty<IWorkflowBlueprint>();
    }
}