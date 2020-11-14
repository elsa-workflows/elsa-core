using System.Threading.Tasks;
using Elsa.Client;
using ElsaDashboard.Shared.Rpc;
using ProtoBuf.Grpc;

namespace ElsaDashboard.Backend.Rpc
{
    public class ActivityService : IActivityService
    {
        private readonly IElsaClient _elsaClient;

        public ActivityService(IElsaClient elsaClient)
        {
            _elsaClient = elsaClient;
        }
        
        public async Task<GetActivitiesResponse> GetActivitiesAsync(CallContext callContext)
        {
            var result = await _elsaClient.Activities.ListAsync(callContext.CancellationToken);
            return new GetActivitiesResponse(result.Items);
        }
    }
}