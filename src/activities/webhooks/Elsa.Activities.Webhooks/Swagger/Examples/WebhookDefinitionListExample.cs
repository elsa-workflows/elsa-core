using System.Collections.Generic;
using System.Linq;
using Elsa.Webhooks.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Activities.Webhooks.Swagger.Examples
{
    public class WebhookDefinitionListExample : IExamplesProvider<IEnumerable<WebhookDefinition>>
    {
        public IEnumerable<WebhookDefinition> GetExamples()
        {
            var result = Enumerable.Range(1, 3).Select(_ => new WebhookDefinitionExample().GetExamples()).ToList();
            return result;
        }
    }
}