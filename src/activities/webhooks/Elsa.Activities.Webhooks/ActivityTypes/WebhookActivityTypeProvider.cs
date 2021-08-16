using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Persistence.Specifications;
using Elsa.Providers.Activities;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Persistence;
using Humanizer;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Webhooks.ActivityTypes
{
    public class WebhookActivityTypeProvider : IActivityTypeProvider
    {
        public const string WebhookMarkerAttribute = "WebhookMarker";
        private const string WebhooksActivityTypeSuffix = "Webhook";
        private const string WebhooksActivityCategory = "Webhooks";

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
            var activityTypeName = webhook.Name.EndsWith(WebhooksActivityTypeSuffix) ? webhook.Name : $"{webhook.Name}{WebhooksActivityTypeSuffix}";
            var activityDisplayName = activityTypeName.Humanize();
            
            ValueTask<ActivityDescriptor> CreateDescriptorAsync()
            {
                var descriptor = new ActivityDescriptor
                {
                    Type = activityTypeName!,
                    DisplayName = activityDisplayName!,
                    Category = WebhooksActivityCategory,
                    Outcomes = new[] { OutcomeNames.Done },
                    Traits = ActivityTraits.Trigger,
                    InputProperties = new[]
                    {
                        new ActivityInputDescriptor(
                            nameof(HttpEndpoint.Path),
                            typeof(PathString),
                            ActivityInputUIHints.SingleLine,
                            "Path",
                            "The relative path that triggers this activity.",
                            null,
                            default,
                            0,
                            webhook.Path,
                            SyntaxNames.Literal,
                            new[] { SyntaxNames.Literal },
                            true
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

                return new ValueTask<ActivityDescriptor>(descriptor);
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
                TypeName = activityTypeName,
                Type = typeof(HttpEndpoint),
                Description = !string.IsNullOrWhiteSpace(webhook.Description) ? webhook.Description : $"A webhook at {webhook.Path}",
                DisplayName = activityDisplayName,
                ActivateAsync = ActivateActivityAsync,
                Attributes = new Dictionary<string, object>
                {
                    [WebhookMarkerAttribute] = true,
                    ["Path"] = webhook.Path,
                },
                DescribeAsync = CreateDescriptorAsync
            };
        }
    }
}