namespace Elsa.Webhooks.Persistence.EntityFramework.Core.Services
{
    public interface IWebhookContextFactory
    {
        WebhookContext CreateDbContext();
    }
}