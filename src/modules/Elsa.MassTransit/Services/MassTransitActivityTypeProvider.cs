using System.ComponentModel;
using System.Reflection;
using Elsa.Extensions;
using Elsa.MassTransit.Activities;
using Elsa.MassTransit.Options;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.MassTransit.Services;

/// <summary>
/// Provides activities to the system from the configured MassTransit message types.
/// </summary>
[UsedImplicitly]
public class MassTransitActivityTypeProvider(IActivityFactory activityFactory, IOptions<MassTransitActivityOptions> options, IActivityDescriber activityDescriber) : IActivityProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var messageTypes = options.Value.MessageTypes;
        var descriptors = await CreateDescriptorsAsync(messageTypes, cancellationToken);
        return descriptors.ToList();
    }

    private async Task<IEnumerable<ActivityDescriptor>> CreateDescriptorsAsync(IEnumerable<Type> messageTypes, CancellationToken cancellationToken = default)
    {
        var descriptors = new List<ActivityDescriptor>();
        foreach (var messageType in messageTypes)
        {
            descriptors.Add(await CreateMessageReceivedDescriptor(messageType, cancellationToken));
            
            if(messageType.IsClass)
                descriptors.Add(await CreatePublishMessageDescriptor(messageType, cancellationToken));
        }
        
        return descriptors;
    }

    private async Task<ActivityDescriptor> CreateMessageReceivedDescriptor(Type messageType, CancellationToken cancellationToken = default)
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
        
        var outputDescriptor = await activityDescriber.DescribeOutputProperty<MessageReceived, object>(x => x.Result!, cancellationToken);
        var openOutputType = typeof(Output<>);
        var outputType = openOutputType.MakeGenericType(messageType);
        outputDescriptor.Type = outputType;

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
                outputDescriptor
            },
            Constructor = context =>
            {
                var activity = activityFactory.Create<MessageReceived>(context);
                activity.Type = fullTypeName;
                activity.MessageType = messageType;
                return activity;
            }
        };
    }

    private async Task<ActivityDescriptor> CreatePublishMessageDescriptor(Type messageType, CancellationToken cancellationToken = default)
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

        var messageInputDescriptor = await activityDescriber.DescribeInputPropertyAsync<PublishMessage, object>(x => x.Message, cancellationToken: cancellationToken);
        var openInputType = typeof(Input<>);
        var inputType = openInputType.MakeGenericType(messageType);
        messageInputDescriptor.Type = inputType;

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
                messageInputDescriptor
            },
            Constructor = context =>
            {
                var activity = activityFactory.Create<PublishMessage>(context);
                activity.Type = fullTypeName;
                activity.MessageType = messageType;
                return activity;
            }
        };
    }
}