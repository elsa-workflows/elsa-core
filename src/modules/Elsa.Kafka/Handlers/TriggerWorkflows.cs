using System.Text;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Kafka.Activities;
using Elsa.Kafka.Notifications;
using Elsa.Kafka.Stimuli;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Kafka.Handlers;

[UsedImplicitly]
public class TriggerWorkflows(
    ITriggerInvoker triggerInvoker,
    IBookmarkQueue bookmarkQueue,
    ICorrelationStrategy correlationStrategy,
    IWorkflowInstanceStore workflowInstanceStore,
    IWorkflowDefinitionService workflowDefinitionService,
    IVariablePersistenceManager variablePersistenceManager,
    IExpressionEvaluator expressionEvaluator,
    IOptions<KafkaOptions> options,
    IServiceProvider serviceProvider,
    ILogger<TriggerWorkflows> logger) : INotificationHandler<TransportMessageReceived>
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

            var isMatch = await EvaluatePredicateAsync(transportMessage, stimulus, null, cancellationToken);

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
            
            var isMatch = await EvaluatePredicateAsync(transportMessage, stimulus, binding, cancellationToken);

            if (isMatch)
                matchingBookmarks.Add(binding);
        }

        return matchingBookmarks;
    }

    private async Task<bool> EvaluatePredicateAsync(KafkaTransportMessage transportMessage, MessageReceivedStimulus stimulus, BookmarkBinding? binding, CancellationToken cancellationToken)
    {
        var predicate = stimulus.Predicate;

        if (predicate == null)
            return true;

        var expressionExecutionContext = await GetExpressionExecutionContextAsync(transportMessage, binding, cancellationToken);

        try
        {
            return await expressionEvaluator.EvaluateAsync<bool>(predicate, expressionExecutionContext);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "An error occurred while evaluating the predicate for stimulus {Stimulus}", stimulus);
            return false;
        }
    }
    
    private async Task<ExpressionExecutionContext> GetExpressionExecutionContextAsync(KafkaTransportMessage transportMessage, BookmarkBinding? binding, CancellationToken cancellationToken)
    {
        var memory = new MemoryRegister();
        var expressionExecutionContext = new ExpressionExecutionContext(serviceProvider, memory, cancellationToken: cancellationToken);
        var transportMessageVariable = new Variable("transportMessage", transportMessage);
        var messageVariable = new Variable("message", transportMessage.Value);
        
        transportMessageVariable.Set(expressionExecutionContext, transportMessage);
        messageVariable.Set(expressionExecutionContext, transportMessage.Value);
        
        if(binding == null)
        {
            return new ExpressionExecutionContext(serviceProvider, memory, cancellationToken: cancellationToken);
        }
        
        var boundWorkflowInstanceId = binding.WorkflowInstanceId;
        var boundWorkflowInstance = await workflowInstanceStore.FindAsync(boundWorkflowInstanceId, cancellationToken);
            
        if (boundWorkflowInstance == null)
        {
            logger.LogWarning("Could not find workflow instance with ID {WorkflowInstanceId}", boundWorkflowInstanceId);
            throw new InvalidOperationException($"Could not find workflow instance with ID {boundWorkflowInstanceId}");
        }
        
        var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(boundWorkflowInstance, cancellationToken);
        var bookmark = workflowExecutionContext.Bookmarks.FirstOrDefault(x => x.Id == binding.BookmarkId) ?? throw new InvalidOperationException($"Could not find bookmark with ID {binding.BookmarkId}");
        var activityInstanceId = bookmark.ActivityInstanceId!;
        var activityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == activityInstanceId) ?? throw new InvalidOperationException($"Could not find activity execution context with ID {activityInstanceId}");
        var parentExecutionContext = activityExecutionContext.ExpressionExecutionContext;
        expressionExecutionContext.ParentContext = parentExecutionContext;
        await variablePersistenceManager.LoadVariablesAsync(workflowExecutionContext);
        
        return expressionExecutionContext;
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
    
    private async Task<WorkflowExecutionContext> CreateWorkflowExecutionContextAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        var workflowDefinition = await workflowDefinitionService.FindWorkflowGraphAsync(workflowInstance.DefinitionVersionId, cancellationToken);

        if (workflowDefinition == null)
            throw new InvalidOperationException($"Could not find workflow definition with ID {workflowInstance.DefinitionVersionId}");

        var workflowState = workflowInstance.WorkflowState;
        return await WorkflowExecutionContext.CreateAsync(serviceProvider, workflowDefinition, workflowState, cancellationToken: cancellationToken);
    }
}