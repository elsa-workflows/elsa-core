using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.ServiceModel;

namespace ElsaDashboard.Shared.Rpc
{
    [ProtoContract]
    public interface IActivityService
    {
        [ProtoBehavior]Task<IEnumerable<ActivityDescriptor>> GetActivitiesAsync(CallContext context = default);
    }
}