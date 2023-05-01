using System.ComponentModel;
using System.Reflection;
using Elsa.Extensions;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Helpers;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Extensions;

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
    public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var payloadTypes = WebhookPayloadTypes.PayloadTypes;
        var descriptors = CreateDescriptors(payloadTypes).ToList();
        return new(descriptors);
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(IEnumerable<Type> jobTypes) => jobTypes.Select(CreateDescriptor);

    private ActivityDescriptor CreateDescriptor(Type payloadType)
    {
        var webhookAttribute = payloadType.GetCustomAttribute<WebhookAttribute>() ?? throw new Exception($"No WebhookAttribute found on payload type {payloadType}");
        var typeName = webhookAttribute.ActivityType;
        var displayNameAttr = payloadType.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = displayNameAttr?.DisplayName ?? webhookAttribute.DisplayName;
        var categoryAttr = payloadType.GetCustomAttribute<CategoryAttribute>();
        var category = categoryAttr?.Category ?? Constants.Category;
        var descriptionAttr = payloadType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? webhookAttribute?.Description;
        var outputPropertyDescriptor = _activityDescriber.DescribeOutputProperty<WebhookEvent, Output<Payload>>(x => x.Result!);

        outputPropertyDescriptor.Type = payloadType;

        return new()
        {
            TypeName = typeName,
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