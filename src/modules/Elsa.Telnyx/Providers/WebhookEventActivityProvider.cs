using System.ComponentModel;
using System.Reflection;
using Elsa.Extensions;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Helpers;
using Elsa.Telnyx.Payloads.Abstractions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management;

namespace Elsa.Telnyx.Providers;

/// <summary>
/// Provides activity descriptors based on Telnyx webhook event payload types (types inheriting <see cref="Payload"/>. 
/// </summary>
public class WebhookEventActivityProvider : IActivityProvider
{
    private readonly IActivityFactory _activityFactory;
    private readonly IActivityDescriber _activityDescriber;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WebhookEventActivityProvider(IActivityFactory activityFactory, IActivityDescriber activityDescriber)
    {
        _activityFactory = activityFactory;
        _activityDescriber = activityDescriber;
    }


    /// <inheritdoc />
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var payloadTypes = WebhookPayloadTypes.PayloadTypes.Where(x => x.GetCustomAttribute<WebhookActivityAttribute>() != null);
        return await CreateDescriptorsAsync(payloadTypes, cancellationToken);
    }

    private async Task<IEnumerable<ActivityDescriptor>> CreateDescriptorsAsync(IEnumerable<Type> payloadTypes, CancellationToken cancellationToken = default)
    {
        return await Task.WhenAll(payloadTypes.Select(async x => await CreateDescriptorAsync(x, cancellationToken)));
    }

    private async Task<ActivityDescriptor> CreateDescriptorAsync(Type payloadType, CancellationToken cancellationToken = default)
    {
        var webhookAttribute = payloadType.GetCustomAttribute<WebhookActivityAttribute>() ?? throw new Exception($"No WebhookActivityAttribute found on payload type {payloadType}");
        var typeName = webhookAttribute.ActivityType;
        var displayNameAttr = payloadType.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = displayNameAttr?.DisplayName ?? webhookAttribute.DisplayName;
        var categoryAttr = payloadType.GetCustomAttribute<CategoryAttribute>();
        var category = categoryAttr?.Category ?? Constants.Category;
        var descriptionAttr = payloadType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? webhookAttribute.Description;
        var outputPropertyDescriptor = await _activityDescriber.DescribeOutputProperty<WebhookEvent, Output<Payload>>(x => x.Result!, cancellationToken);

        outputPropertyDescriptor.Type = payloadType;

        return new()
        {
            TypeName = typeName,
            Name = typeName,
            Version = 1,
            DisplayName = displayName,
            Description = description,
            Category = category,
            Kind = ActivityKind.Trigger,
            IsBrowsable = true,
            Attributes = { webhookAttribute! },
            Outputs = { outputPropertyDescriptor },
            Constructor = context =>
            {
                var activity = _activityFactory.Create<WebhookEvent>(context);
                activity.Type = typeName;
                activity.EventType = webhookAttribute!.EventType;

                return activity;
            }
        };
    }
}