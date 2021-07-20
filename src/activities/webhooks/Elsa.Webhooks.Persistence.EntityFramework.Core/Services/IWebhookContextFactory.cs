using Elsa.Persistence.EntityFramework.Core.Services;

namespace Elsa.Webhooks.Persistence.EntityFramework.Core.Services
{
    public interface IWebhookContextFactory : IContextFactory<WebhookContext>
    {
    }
}