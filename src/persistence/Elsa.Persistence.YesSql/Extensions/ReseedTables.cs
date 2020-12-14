using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using YesSql;

namespace Elsa.Persistence.YesSql.Extensions
{
    public class ReseedTables : IStartupTask
    {
        private readonly ISession _session;

        public ReseedTables(ISession session)
        {
            _session = session;
        }
        
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            // using (var transaction = await _session.DemandAsync())
            // {
            //     // TODO: re-seed primary key values for tables that are empty (especially SuspendedWorkflows_SuspendedWorkflowIndex)
            // }
        }
    }
}