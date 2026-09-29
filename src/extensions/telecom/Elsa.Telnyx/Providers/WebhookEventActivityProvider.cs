using System.ComponentModel;
using System.Reflection;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Helpers;
using Elsa.Telnyx.Payloads.Abstractions;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;

namespace Elsa.Telnyx.Providers;

/// <summary>
/// Provides activity descriptors based on Telnyx webhook event payload types (types inheriting <see cref="Payload"/>. 
/// </summary>
public class WebhookEventActivityProvider(IActivityDescriber activityDescriber) : IActivityProvider
{
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
        var webhookAttribute = payloadType.GetCustomAttribute<WebhookActivityAttribute>() ?? throw new($"No WebhookActivityAttribute found on payload type {payloadType}");
        var typeName = webhookAttribute.ActivityType;
        var displayNameAttr = payloadType.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = displayNameAttr?.DisplayName ?? webhookAttribute.DisplayName;
        var categoryAttr = payloadType.GetCustomAttribute<CategoryAttribute>();
        var category = categoryAttr?.Category ?? Constants.Category;
        var descriptionAttr = payloadType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? webhookAttribute.Description;
        var outputPropertyDescriptor = await activityDescriber.DescribeOutputProperty<WebhookEvent, Output<Payload>>(x => x.Result!, cancellationToken);

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
                var result = context.CreateActivity<WebhookEvent>();
                var activity = result.Activity;
                activity.Type = typeName;
                activity.EventType = webhookAttribute!.EventType;
                return result;
            }
        };
    }
}