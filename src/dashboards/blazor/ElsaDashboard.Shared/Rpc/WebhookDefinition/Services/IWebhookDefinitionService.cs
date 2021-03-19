using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.Webhooks.Models;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace ElsaDashboard.Shared.Rpc
{
    [Service]
    public interface IWebhookDefinitionService
    {
        [Operation]
        Task<IEnumerable<WebhookDefinition>> ListAsync(CallContext context = default);

        [Operation]
        Task<WebhookDefinition> GetByIdAsync(GetWebhookDefinitionByIdRequest request, CallContext context = default);
    }
}