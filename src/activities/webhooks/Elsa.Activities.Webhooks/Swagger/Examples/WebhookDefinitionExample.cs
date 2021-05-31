using System;
using Elsa.Activities.Webhooks.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Swagger.Examples
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