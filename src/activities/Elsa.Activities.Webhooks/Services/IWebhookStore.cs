using System;
using Elsa.Activities.Webhooks.Models;
using Elsa.Persistence;

namespace Elsa.Activities.Webhooks.Services
{
    public interface IWebhookStore : IStore<Webhook>
    {
    }
}
