using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Server.Api.Services
{
    public interface IServerFeatureProvider
    {
        ValueTask<IEnumerable<string>> GetFeaturesAsync(CancellationToken cancellationToken);
    }
}