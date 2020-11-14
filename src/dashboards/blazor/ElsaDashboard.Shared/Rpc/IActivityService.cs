using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.ServiceModel;

namespace ElsaDashboard.Shared.Rpc
{
    [ServiceContract]
    public interface IActivityService
    {
        [ProtoBehavior]
        Task<GetActivitiesResponse> GetActivitiesAsync(CallContext context = default);
    }

    [ProtoContract]
    public sealed class GetActivitiesResponse
    {
        public GetActivitiesResponse()
        {
        }

        public GetActivitiesResponse(IEnumerable<ActivityDescriptor> activities)
        {
            Activities = activities;
        }
        
        [ProtoMember(1)] public IEnumerable<ActivityDescriptor> Activities { get; set; } = new System.Collections.Generic.List<ActivityDescriptor>();
    }
}