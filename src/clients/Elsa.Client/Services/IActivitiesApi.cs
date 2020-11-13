using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using Refit;

namespace Elsa.Client.Services
{
    public interface IActivitiesApi
    {
        [Get("/v1/activities")]
        Task<List<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken = default);
    }
}