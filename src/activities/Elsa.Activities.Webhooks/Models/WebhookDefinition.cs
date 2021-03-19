using System;
using Elsa.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Webhooks.Models
{
    public class WebhookDefinition : Entity, ITenantScope
    {
        public string Name { get; set; } = default!;

        public PathString Path { get; set; }

        public string? Description { get; set; }

        public string? PayloadTypeName { get; set; }

        public string? TenantId { get; set; }
    }
}
