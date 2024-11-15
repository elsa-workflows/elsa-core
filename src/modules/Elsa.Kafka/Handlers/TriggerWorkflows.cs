using System.Text;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Kafka.Activities;
using Elsa.Kafka.Notifications;
using Elsa.Kafka.Stimuli;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Kafka.Handlers;

[UsedImplicitly]
public class TriggerWorkflows(
    ITriggerInvoker triggerInvoker,
    IBookmarkQueue bookmarkQueue,
    ICorrelationStrategy correlationStrategy,
    IExpressionEvaluator expressionEvaluator,
    IOptions<KafkaOptions> options,
    IServiceProvider serviceProvider) : INotificationHandler<TransportMessageReceived>
{
    private static readonly string MessageReceivedActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<MessageReceived>();

    public async Task HandleAsync(TransportMessageReceived notification, CancellationToken cancellationToken)
    {
        var worker = notification.Worker;
        var consumerDefinition = worker.ConsumerDefinition;
        var consumerDefinitionId = consumerDefinition.Id;
        var transportMessage = notification.TransportMessage;
        var boundTriggers = worker.TriggerBindings.Values;
        var boundBookmarks = worker.BookmarkBindings.Values;
        var matchingTriggers = await GetMatchingTriggerBindingsAsync(boundTriggers, consumerDefinitionId, transportMessage, cancellationToken);
        var matchingBookmarks = await GetMatchingBookmarkBindingsAsync(boundBookmarks, consumerDefinitionId, transportMessage, cancellationToken);

        await InvokeTriggersAsync(matchingTriggers, transportMessage, cancellationToken);
        await InvokeBookmarksAsync(matchingBookmarks, transportMessage, cancellationToken);
    }

    private async Task InvokeTriggersAsync(IEnumerable<TriggerBinding> matchingTriggers, KafkaTransportMessage transportMessage, CancellationToken cancellationToken)
    {
        var input = new Dictionary<string, object>
        {
            [MessageReceived.InputKey] = transportMessage
        };

        foreach (var binding in matchingTriggers)
        {
            var invokeTriggerRequest = new InvokeTriggerRequest
            {
                Workflow = binding.Workflow,
                ActivityId = binding.TriggerActivityId,
                CorrelationId = GetCorrelationId(transportMessage),
                Input = input,
                Properties = new Dictionary<string, object>
                {
                    [MessageReceived.InputKey] = transportMessage
                }
            };
            await triggerInvoker.InvokeAsync(invokeTriggerRequest, cancellationToken);
        }
    }

    private async Task InvokeBookmarksAsync(IEnumerable<BookmarkBinding> matchingBookmarks, KafkaTransportMessage transportMessage, CancellationToken cancellationToken)
    {
        var input = new Dictionary<string, object>
        {
            [MessageReceived.InputKey] = transportMessage
        };

        var properties = new Dictionary<string, object>
        {
            [MessageReceived.InputKey] = transportMessage
        };

        foreach (var binding in matchingBookmarks)
        {
            var bookmarkQueueItem = new NewBookmarkQueueItem
            {
                WorkflowInstanceId = binding.WorkflowInstanceId,
                BookmarkId = binding.BookmarkId,
                Options = new ResumeBookmarkOptions
                {
                    Input = input,
                    Properties = properties
                },
                ActivityTypeName = MessageReceivedActivityTypeName
            };
            await bookmarkQueue.EnqueueAsync(bookmarkQueueItem, cancellationToken);
        }
    }

    private async Task<IEnumerable<TriggerBinding>> GetMatchingTriggerBindingsAsync(
        IEnumerable<TriggerBinding> boundTriggers,
        string consumerDefinitionId,
        KafkaTransportMessage transportMessage,
        CancellationToken cancellationToken)
    {
        var matchingTriggers = new List<TriggerBinding>();
        var topic = GetTopic(transportMessage);

        if (string.IsNullOrEmpty(topic))
            return matchingTriggers;

        foreach (var binding in boundTriggers)
        {
            var isMatch = await IsMatchAsync(consumerDefinitionId, transportMessage, binding.Stimulus, topic, cancellationToken);

            if (isMatch)
                matchingTriggers.Add(binding);
        }

        return matchingTriggers;
    }

    private async Task<IEnumerable<BookmarkBinding>> GetMatchingBookmarkBindingsAsync(
        IEnumerable<BookmarkBinding> boundBookmarks,
        string consumerDefinitionId,
        KafkaTransportMessage transportMessage,
        CancellationToken cancellationToken)
    {
        var matchingBookmarks = new List<BookmarkBinding>();
        var topic = GetTopic(transportMessage);

        if (string.IsNullOrEmpty(topic))
            return matchingBookmarks;

        foreach (var binding in boundBookmarks)
        {
            var isMatch = await IsMatchAsync(consumerDefinitionId, transportMessage, binding.Stimulus, topic, cancellationToken);

            if (isMatch)
                matchingBookmarks.Add(binding);
        }

        return matchingBookmarks;
    }

    private async Task<bool> IsMatchAsync(string consumerDefinitionId, KafkaTransportMessage transportMessage, MessageReceivedStimulus stimulus, string topic, CancellationToken cancellationToken)
    {
        if (stimulus.ConsumerDefinitionId != consumerDefinitionId)
            return false;

        if (stimulus.Topics.All(x => x != topic))
            return false;

        return await EvaluatePredicateAsync(transportMessage, stimulus, cancellationToken);
    }

    private async Task<bool> EvaluatePredicateAsync(KafkaTransportMessage transportMessage, MessageReceivedStimulus stimulus, CancellationToken cancellationToken)
    {
        var predicate = stimulus.Predicate;

        if (predicate == null)
            return true;

        var memory = new MemoryRegister();
        var messageVariable = new Variable("message", transportMessage);
        var messageType = stimulus.MessageType;
        var message = DeserializeMessage(transportMessage.Value, messageType);
        var expressionExecutionContext = new ExpressionExecutionContext(serviceProvider, memory, cancellationToken: cancellationToken);
        messageVariable.Set(expressionExecutionContext, message);
        return await expressionEvaluator.EvaluateAsync<bool>(predicate, expressionExecutionContext);
    }

    private object DeserializeMessage(string value, Type? type)
    {
        return type == null ? value : options.Value.Deserializer(serviceProvider, value, type);
    }

    private string? GetCorrelationId(KafkaTransportMessage transportMessage)
    {
        return correlationStrategy.GetCorrelationId(transportMessage);
    }

    private string? GetTopic(KafkaTransportMessage transportMessage)
    {
        var topicHeaderKey = options.Value.TopicHeaderKey;
        return transportMessage.Headers.TryGetValue(topicHeaderKey, out var topicBytes) ? Encoding.UTF8.GetString(topicBytes) : null;
    }
}