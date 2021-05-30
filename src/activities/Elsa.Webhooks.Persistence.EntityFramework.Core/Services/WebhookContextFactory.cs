using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.Core.Services
{
    public class WebhookContextFactory<TElsaContext> : IWebhookContextFactory where TElsaContext : WebhookContext
    {
        private readonly IDbContextFactory<TElsaContext> _contextFactory;
        public WebhookContextFactory(IDbContextFactory<TElsaContext> contextFactory) => _contextFactory = contextFactory;
        public WebhookContext CreateDbContext() => _contextFactory.CreateDbContext();
    }
}