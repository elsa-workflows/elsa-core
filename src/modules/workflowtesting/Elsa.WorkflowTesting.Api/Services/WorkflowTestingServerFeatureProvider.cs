using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Api.Services;

namespace Elsa.WorkflowTesting.Api.Services
{
    public class WorkflowTestingServerFeatureProvider : IServerFeatureProvider
    {
        public ValueTask<IEnumerable<string>> GetFeaturesAsync(CancellationToken cancellationToken) => ValueTask.FromResult<IEnumerable<string>>(new[] { "WorkflowTesting" });
    }
}