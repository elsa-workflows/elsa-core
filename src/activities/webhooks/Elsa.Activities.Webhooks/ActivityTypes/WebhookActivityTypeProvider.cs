using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http;
using Elsa.Webhooks.Abstractions.Models;
using Elsa.Webhooks.Abstractions.Persistence;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Webhooks.ActivityTypes
{
    public class WebhookActivityTypeProvider : IActivityTypeProvider
    {
        public const string WebhookMarkerAttribute = "WebhookMarker";
        private const string WebhookActivityCategory = "Webhooks";

        private readonly IWebhookDefinitionStore _webhookDefinitionStore;
        private readonly IActivityActivator _activityActivator;

        public WebhookActivityTypeProvider(
            IWebhookDefinitionStore webhookDefinitionStore,
            IActivityActivator activityActivator)
        {
            _webhookDefinitionStore = webhookDefinitionStore;
            _activityActivator = activityActivator;
        }

        public async ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken = default)
        {
            var specification = Specification<WebhookDefinition>.Identity;
            var definitions = await _webhookDefinitionStore.FindManyAsync(specification, cancellationToken: cancellationToken);

            var activityTypes = new List<ActivityType>();
            foreach (var definition in definitions)
            {
                var activity = CreateWebhookActivityType(definition);
                activityTypes.Add(activity);
            }

            return activityTypes;
        }

        private ActivityType CreateWebhookActivityType(WebhookDefinition webhook)
        {
            var typeName = webhook.Name;
            var displayName = webhook.Name;

            var descriptor = new ActivityDescriptor
            {
                Type = typeName,
                DisplayName = displayName,
                Category = WebhookActivityCategory,
                Outcomes = new[] { OutcomeNames.Done },
                Traits = ActivityTraits.Trigger,
                InputProperties = new[]
                {
                    new ActivityInputDescriptor(
                        nameof(HttpEndpoint.Path),
                        typeof(Microsoft.AspNetCore.Http.PathString),
                        ActivityInputUIHints.SingleLine,
                        "Path",
                        "The relative path that triggers this activity.",
                        null,
                        default,
                        0,
                        webhook.Path,
                        SyntaxNames.Literal,
                        new[] { SyntaxNames.Literal }
                        ),
                    new ActivityInputDescriptor(
                        nameof(HttpEndpoint.Methods),
                        typeof(HashSet<string>),
                        ActivityInputUIHints.CheckList,
                        "Request Method",
                        "Specify what request method this webhook should handle. Leave empty to handle both GET and POST requests",
                        new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD" },
                        default,
                        1,
                        new[] { "GET", "POST" },
                        SyntaxNames.Json,
                        new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
                        )
                },
                OutputProperties = new[]
                {
                    new ActivityOutputDescriptor
                    (
                       "Request",
                       typeof(Models.WebhookRequestModel),
                       "The received HTTP request."
                    )
                }
            };

            async ValueTask<IActivity> ActivateActivityAsync(ActivityExecutionContext context)
            {
                var activity = await _activityActivator.ActivateActivityAsync<HttpEndpoint>(context);

                activity.Path = webhook.Path;
                activity.TargetType = !string.IsNullOrWhiteSpace(webhook.PayloadTypeName) ? Type.GetType(webhook.PayloadTypeName) ?? throw new Exception($"Type {webhook.PayloadTypeName} not found") : null;
                return activity;
            }

            return new ActivityType
            {
                TypeName = webhook.Name,
                Type = typeof(HttpEndpoint),
                Description = !string.IsNullOrWhiteSpace(webhook.Description) ? webhook.Description : $"A webhook at {webhook.Path}",
                DisplayName = webhook.Name,
                ActivateAsync = ActivateActivityAsync,
                Attributes = new Dictionary<string, object>
                {
                    [WebhookMarkerAttribute] = true,
                    ["Path"] = webhook.Path,
                },
                Describe = () => descriptor
            };
        }
    }
}