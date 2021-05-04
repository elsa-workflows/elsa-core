using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Client.Models;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

// ReSharper disable once CheckNamespace
namespace ElsaDashboard.Shared.Rpc
{
    [Service]
    public interface IActivityService
    {
        [Operation]
        Task<IEnumerable<ActivityDescriptor>> GetActivitiesAsync(CallContext context = default);
    }
}