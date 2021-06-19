using Elsa.Persistence.Specifications;
using System;
using System.Linq.Expressions;
using Elsa.Webhooks.Models;

namespace Elsa.Activities.Webhooks.Persistence.Specifications.WebhookDefinitions
{
    public class WebhookIdSpecification : Specification<WebhookDefinition>
    {
        public string Id { get; set; }

        public WebhookIdSpecification(string id)
        {
            Id = id;
        }

        public override Expression<Func<WebhookDefinition, bool>> ToExpression() => x => x.Id == Id;
    }
}