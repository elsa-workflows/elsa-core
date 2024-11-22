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
        var topic = transportMessage.Topic;

        if (string.IsNullOrEmpty(topic))
            return matchingTriggers;

        foreach (var binding in boundTriggers)
        {
            var stimulus = binding.Stimulus;
            if (stimulus.ConsumerDefinitionId != consumerDefinitionId)
                continue;

            if (stimulus.Topics.All(x => x != topic))
                continue;

            var isMatch = await EvaluatePredicateAsync(transportMessage, stimulus, cancellationToken);

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
        var topic = transportMessage.Topic;

        if (string.IsNullOrEmpty(topic))
            return matchingBookmarks;

        var correlationId = GetCorrelationId(transportMessage);
        var workflowInstanceId = GetWorkflowInstanceId(transportMessage);
        
        foreach (var binding in boundBookmarks)
        {
            var stimulus = binding.Stimulus;
            if (stimulus.ConsumerDefinitionId != consumerDefinitionId)
                continue;

            if (stimulus.Topics.All(x => x != topic))
                continue;

            if (stimulus.IsLocal)
            {
                if (binding.WorkflowInstanceId != workflowInstanceId)
                {
                    if (string.IsNullOrWhiteSpace(correlationId))
                        continue;

                    if (binding.CorrelationId != correlationId)
                        continue;
                }
            }

            var isMatch = await EvaluatePredicateAsync(transportMessage, stimulus, cancellationToken);

            if (isMatch)
                matchingBookmarks.Add(binding);
        }

        return matchingBookmarks;
    }

    private async Task<bool> EvaluatePredicateAsync(KafkaTransportMessage transportMessage, MessageReceivedStimulus stimulus, CancellationToken cancellationToken)
    {
        var predicate = stimulus.Predicate;

        if (predicate == null)
            return true;

        var memory = new MemoryRegister();
        var messageVariable = new Variable("message", transportMessage);
        var message = transportMessage;
        var expressionExecutionContext = new ExpressionExecutionContext(serviceProvider, memory, cancellationToken: cancellationToken);
        messageVariable.Set(expressionExecutionContext, message);
        return await expressionEvaluator.EvaluateAsync<bool>(predicate, expressionExecutionContext);
    }
    
    private string? GetWorkflowInstanceId(KafkaTransportMessage transportMessage)
    {
        var key = options.Value.WorkflowInstanceIdHeaderKey;
        return transportMessage.Headers.TryGetValue(key, out var value) ? Encoding.UTF8.GetString(value) : null;
    }

    private string? GetCorrelationId(KafkaTransportMessage transportMessage)
    {
        return correlationStrategy.GetCorrelationId(transportMessage);
    }
}