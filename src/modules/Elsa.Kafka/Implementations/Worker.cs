using Confluent.Kafka;
using Elsa.Extensions;
using Elsa.Kafka.Notifications;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Kafka.Implementations;

public class Worker<TKey, TValue>(WorkerContext workerContext, IConsumer<TKey, TValue> consumer, ILogger<Worker<TKey, TValue>> logger) : IWorker
{
    private bool _running;
    private CancellationTokenSource _cancellationTokenSource = new();
    private HashSet<string> _subscribedTopics = new();

    public IDictionary<string, BookmarkBinding> BookmarkBindings { get; } = new Dictionary<string, BookmarkBinding>();
    public IDictionary<string, TriggerBinding> TriggerBindings { get; } = new Dictionary<string, TriggerBinding>();
    public ConsumerDefinition ConsumerDefinition { get; } = workerContext.ConsumerDefinition;

    public void Start(CancellationToken cancellationToken)
    {
        if (_running)
            return;

        _running = true;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = Task.Run(() => RunAsync(_cancellationTokenSource.Token), cancellationToken);
    }

    public void Stop()
    {
        if (!_running)
            return;

        _running = false;
        consumer.Unsubscribe();
        consumer.Close();
        consumer.Dispose();
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    public void BindTrigger(TriggerBinding binding)
    {
        TriggerBindings[binding.TriggerId] = binding;
        ReduceTopicSubscriptions();
    }

    public void BindBookmark(BookmarkBinding binding)
    {
        BookmarkBindings[binding.BookmarkId] = binding;
        ReduceTopicSubscriptions();
    }

    public void RemoveTriggers(IEnumerable<string> triggerIds)
    {
        var triggerIdList = triggerIds.ToList();
        TriggerBindings.RemoveWhere(x => triggerIdList.Contains(x.Key));
        ReduceTopicSubscriptions();
    }

    public void RemoveBookmarks(IEnumerable<string> bookmarkIds)
    {
        var bookmarkIdList = bookmarkIds.ToList();
        BookmarkBindings.RemoveWhere(x => bookmarkIdList.Contains(x.Key));
        ReduceTopicSubscriptions();
    }

    private void ReduceTopicSubscriptions()
    {
        var bookmarkTopics = BookmarkBindings.Values.SelectMany(x => x.Stimulus.Topics).Distinct().ToList();
        var triggerTopics = TriggerBindings.Values.SelectMany(x => x.Stimulus.Topics).Distinct().ToList();
        var allTopics = bookmarkTopics.Concat(triggerTopics).Distinct().ToList();
        Subscribe(allTopics);
    }

    public void Dispose()
    {
        Stop();
    }

    private void Subscribe(IEnumerable<string> topics)
    {
        if (!_running)
            throw new InvalidOperationException("The worker is not running.");

        var topicList = topics.ToHashSet();

        if (topicList.SetEquals(_subscribedTopics))
            return;

        _subscribedTopics = topicList.ToHashSet();
        if(_subscribedTopics.Any())
            consumer.Subscribe(_subscribedTopics);
        else
            consumer.Unsubscribe();

        logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", _subscribedTopics));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var consumeExceptionCount = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(cancellationToken);
                consumeExceptionCount = 0;

                if (consumeResult.IsPartitionEOF)
                    continue;

                await ProcessMessageAsync(consumeResult, cancellationToken);
            }
            catch (ConsumeException e)
            {
                logger.LogWarning(e, "Error consuming message.");
                consumeExceptionCount++;

                if (consumeExceptionCount > 100)
                    throw new InvalidOperationException("Too many consume exceptions.");
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Consumer was cancelled.");
                break;
            }
        }

        consumer.Unsubscribe();
        consumer.Close();
        consumer.Dispose();
    }

    private async Task ProcessMessageAsync(ConsumeResult<TKey, TValue> consumeResult, CancellationToken cancellationToken)
    {
        var message = consumeResult.Message;
        var topic = consumeResult.Topic;
        var headers = message.Headers.ToDictionary(x => x.Key, x => x.GetValueBytes());
        var notification = new TransportMessageReceived(this, new KafkaTransportMessage(message.Key, message.Value, topic, headers));
        await using var scope = workerContext.ScopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.SendAsync(notification, cancellationToken);
    }
}