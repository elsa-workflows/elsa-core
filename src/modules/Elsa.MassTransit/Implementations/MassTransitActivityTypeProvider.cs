using System.ComponentModel;
using System.Reflection;
using Elsa.Extensions;
using Elsa.MassTransit.Activities;
using Elsa.MassTransit.Options;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Humanizer;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.Implementations;

/// <summary>
/// Provides activities to the system from the configured MassTransit message types.
/// </summary>
public class MassTransitActivityTypeProvider : IActivityProvider
{
    private readonly IActivityFactory _activityFactory;
    private readonly MassTransitActivityOptions _options;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MassTransitActivityTypeProvider(IActivityFactory activityFactory, IOptions<MassTransitActivityOptions> options)
    {
        _activityFactory = activityFactory;
        _options = options.Value;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var messageTypes = _options.MessageTypes;
        var descriptors = CreateDescriptors(messageTypes).ToList();
        return new(descriptors);
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(IEnumerable<Type> messageTypes)
    {
        foreach (var messageType in messageTypes)
        {
            yield return CreateMessageReceivedDescriptor(messageType);
            
            if(messageType.IsClass)
                yield return CreatePublishMessageDescriptor(messageType);
        }
    }

    private ActivityDescriptor CreateMessageReceivedDescriptor(Type messageType)
    {
        var activityAttr = messageType.GetCustomAttribute<ActivityAttribute>();
        var typeName = activityAttr?.Type ?? messageType.Name;
        var fullTypeName = ActivityTypeNameHelper.GenerateTypeName(messageType);
        var displayNameAttr = messageType.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = displayNameAttr?.DisplayName ?? activityAttr?.DisplayName ?? typeName.Humanize(LetterCasing.Title);
        var categoryAttr = messageType.GetCustomAttribute<CategoryAttribute>();
        var category = categoryAttr?.Category ?? activityAttr?.Category ?? "MassTransit";
        var descriptionAttr = messageType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? activityAttr?.Description;

        return new()
        {
            TypeName = fullTypeName,
            Version = 1,
            DisplayName = displayName,
            Description = description,
            Category = category,
            Kind = ActivityKind.Trigger,
            IsBrowsable = true,
            Outputs =
            {
                new OutputDescriptor
                {
                    Description = "The received message",
                    DisplayName = "Received Message",
                    Name = nameof(MessageReceived.Result),
                    Type = typeof(object)
                }
            },
            Constructor = context =>
            {
                var activity = _activityFactory.Create<MessageReceived>(context);
                activity.Type = fullTypeName;
                activity.MessageType = messageType;
                return activity;
            }
        };
    }

    private ActivityDescriptor CreatePublishMessageDescriptor(Type messageType)
    {
        var activityAttr = messageType.GetCustomAttribute<ActivityAttribute>();
        var typeName = activityAttr?.Type ?? messageType.Name;
        var ns = ActivityTypeNameHelper.GenerateNamespace(messageType);
        var fullTypeName = ns + ".Publish" + typeName;
        var displayNameAttr = messageType.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = "Publish " + (displayNameAttr?.DisplayName ?? activityAttr?.DisplayName ?? typeName.Humanize(LetterCasing.Title));
        var categoryAttr = messageType.GetCustomAttribute<CategoryAttribute>();
        var category = categoryAttr?.Category ?? activityAttr?.Category ?? "MassTransit";
        var descriptionAttr = messageType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? activityAttr?.Description;

        return new()
        {
            TypeName = fullTypeName,
            Version = 1,
            DisplayName = displayName,
            Description = description,
            Category = category,
            Kind = ActivityKind.Action,
            IsBrowsable = true,
            Inputs =
            {
                new InputDescriptor
                {
                    Description = "The message to publish.",
                    UIHint = InputUIHints.MultiLine,
                    DisplayName = "Message",
                    Type = typeof(Input<object>),
                    Name = nameof(PublishMessage.Message)
                }
            },
            Constructor = context =>
            {
                var activity = _activityFactory.Create<PublishMessage>(context);
                activity.Type = fullTypeName;
                activity.MessageType = messageType;
                return activity;
            }
        };
    }
}