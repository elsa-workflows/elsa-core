using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using Refit;

namespace Elsa.Client.Services
{
    public interface IActivitiesApi
    {
        [Get("/{tenant}/v1/activities")]
        Task<ICollection<ActivityDescriptor>> ListAsync(string tenant, CancellationToken cancellationToken = default);
    }
}