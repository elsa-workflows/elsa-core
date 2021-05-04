using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Client;
using Elsa.Client.Models;
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

        public async Task<IEnumerable<ActivityDescriptor>> GetActivitiesAsync(CallContext callContext) => await _elsaClient.Activities.ListAsync(callContext.CancellationToken);
    }
}