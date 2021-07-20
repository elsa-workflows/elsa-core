using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.Core.Services
{
    public class WebhookContextFactory<TWebhookContext> : IWebhookContextFactory where TWebhookContext : WebhookContext
    {
        private readonly IDbContextFactory<TWebhookContext> _contextFactory;
        public WebhookContextFactory(IDbContextFactory<TWebhookContext> contextFactory) => _contextFactory = contextFactory;
        public WebhookContext CreateDbContext() => _contextFactory.CreateDbContext();
    }
}