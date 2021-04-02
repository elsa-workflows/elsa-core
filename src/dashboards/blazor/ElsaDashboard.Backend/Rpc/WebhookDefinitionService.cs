using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Client;
using Elsa.Client.Webhooks.Models;
using ElsaDashboard.Shared.Rpc;
using ProtoBuf.Grpc;

namespace ElsaDashboard.Backend.Rpc
{
    public class WebhookDefinitionService : IWebhookDefinitionService
    {
        private readonly IElsaClient _elsaClient;

        public WebhookDefinitionService(IElsaClient elsaClient)
        {
            _elsaClient = elsaClient;
        }

        public Task<IEnumerable<WebhookDefinition>> ListAsync(CallContext context = default) =>
            _elsaClient.WebhookDefinitions.ListAsync(context.CancellationToken);

        public Task<WebhookDefinition> GetByIdAsync(GetWebhookDefinitionByIdRequest request, CallContext context = default) =>
            _elsaClient.WebhookDefinitions.GetByIdAsync(request.WebhookDefinitionId, context.CancellationToken);
    }
}