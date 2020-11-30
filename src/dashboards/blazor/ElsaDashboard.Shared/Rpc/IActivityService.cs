using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace ElsaDashboard.Shared.Rpc
{
    [Service]
    public interface IActivityService
    {
        Task<IEnumerable<ActivityInfo>> GetActivitiesAsync(CallContext context = default);
    }
}