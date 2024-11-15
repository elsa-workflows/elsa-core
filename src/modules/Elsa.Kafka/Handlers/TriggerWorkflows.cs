using System.Text;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Kafka.Activities;
using Elsa.Kafka.Notifications;
using Elsa.Kafka.Stimuli;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Kafka.Handlers;

[UsedImplicitly]
public class TriggerWorkflows(
    ITriggerInvoker triggerInvoker,
    IBookmarkResumer bookmarkResumer,
    ICorrelationStrategy correlationStrategy, 
    IExpressionEvaluator expressionEvaluator, 
    IOptions<KafkaOptions> options, 
    IServiceProvider serviceProvider) : INotificationHandler<TransportMessageReceived>
{
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
        
        foreach (var binding in matchingBookmarks)
        {
            var invokeBookmarkRequest = new ResumeBookmarkRequest
            {
                WorkflowInstanceId = binding.WorkflowInstanceId,
                BookmarkId = binding.BookmarkId,
                Input = input,
                Properties = new Dictionary<string, object>
                {
                    [MessageReceived.InputKey] = transportMessage
                }
            };
            await bookmarkResumer.ResumeAsync(invokeBookmarkRequest, cancellationToken);
        }
    }

    private async Task<IEnumerable<TriggerBinding>> GetMatchingTriggerBindingsAsync(
        IEnumerable<TriggerBinding> boundTriggers, 
        string consumerDefinitionId, 
        KafkaTransportMessage transportMessage, 
        CancellationToken cancellationToken)
    {
        var matchingTriggers = new List<TriggerBinding>();
        var topicHeaderKey = options.Value.TopicHeaderKey;
        var topic = transportMessage.Headers.TryGetValue(topicHeaderKey, out var topicBytes) ? Encoding.UTF8.GetString(topicBytes) : null;
        
        if(string.IsNullOrEmpty(topic))
            return matchingTriggers;
        
        foreach (var binding in boundTriggers)
        {
            if(binding.Stimulus.ConsumerDefinitionId != consumerDefinitionId)
                continue;
            
            if(binding.Stimulus.Topics.All(x => x != topic))
                continue;
            
            var isMatch = await EvaluatePredicateAsync(transportMessage, binding.Stimulus, cancellationToken);
            
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
        foreach (var binding in boundBookmarks)
        {
            if(binding.Stimulus.ConsumerDefinitionId != consumerDefinitionId)
                continue;
            
            var isMatch = await EvaluatePredicateAsync(transportMessage, binding.Stimulus, cancellationToken);
            
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
}