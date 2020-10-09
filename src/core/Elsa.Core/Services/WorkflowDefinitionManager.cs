using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;
using YesSql;

namespace Elsa.Services
{
    public class WorkflowDefinitionManager : IWorkflowDefinitionManager
    {
        private readonly ISession _session;

        public WorkflowDefinitionManager(ISession session)
        {
            _session = session;
        }
        
        public async Task<IEnumerable<WorkflowDefinition>> ListAsync(
            VersionOptions version,
            CancellationToken cancellationToken)
        {
            var query = _session.Query<WorkflowDefinition>().WithVersion(version);
            return await query.ListAsync();
        }
    }
}