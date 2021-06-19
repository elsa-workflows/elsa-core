using Elsa.Webhooks.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Webhooks.Persistence.EntityFramework.Core.Configuration
{
    public class WebhookDefinitionConfiguration : IEntityTypeConfiguration<WebhookDefinition>
    {
        public void Configure(EntityTypeBuilder<WebhookDefinition> builder)
        {
            builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(WebhookDefinition)}_{nameof(WebhookDefinition.TenantId)}");
            builder.HasIndex(x => x.Path).HasDatabaseName($"IX_{nameof(WebhookDefinition)}_{nameof(WebhookDefinition.Path)}");
            builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WebhookDefinition)}_{nameof(WebhookDefinition.Name)}");
            builder.HasIndex(x => x.Description).HasDatabaseName($"IX_{nameof(WebhookDefinition)}_{nameof(WebhookDefinition.Description)}");
            builder.HasIndex(x => x.PayloadTypeName).HasDatabaseName($"IX_{nameof(WebhookDefinition)}_{nameof(WebhookDefinition.PayloadTypeName)}");
            builder.HasIndex(x => x.IsEnabled).HasDatabaseName($"IX_{nameof(WebhookDefinition)}_{nameof(WebhookDefinition.IsEnabled)}");
        }
    }
}