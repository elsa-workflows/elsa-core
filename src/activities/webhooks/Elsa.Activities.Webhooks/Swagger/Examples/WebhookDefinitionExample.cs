using System;
using Elsa.Webhooks.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Activities.Webhooks.Swagger.Examples
{
    public class WebhookDefinitionExample : IExamplesProvider<WebhookDefinition>
    {
        public WebhookDefinition GetExamples()
        {
            return new()
            {
                Id = Guid.NewGuid().ToString("N"),
                Path = "/sample-path",
                Description = "Sample description",
                PayloadTypeName = "PayloadType",
                TenantId = "tenant",
            };
        }
    }
}