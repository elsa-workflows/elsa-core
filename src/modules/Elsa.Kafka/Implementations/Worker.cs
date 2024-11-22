using Confluent.Kafka;
using Elsa.Extensions;
using Elsa.Kafka.Notifications;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Kafka.Implementations;

public class Worker<TKey, TValue>(WorkerContext workerContext, IConsumer<TKey, TValue> consumer) : IWorker
{
    private bool _running;
    private CancellationTokenSource _cancellationTokenSource = new();
    private readonly HashSet<string> _subscribedTopics = new();
    
    public IDictionary<string, BookmarkBinding> BookmarkBindings { get;  } = new Dictionary<string, BookmarkBinding>();
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
        var topics = binding.Stimulus.Topics.Distinct().ToList();
        TriggerBindings[binding.TriggerId] = binding;
        Subscribe(topics);
    }

    public void BindBookmark(BookmarkBinding binding)
    {
        var topics = binding.Stimulus.Topics.Distinct().ToList();
        BookmarkBindings[binding.BookmarkId] = binding;
        Subscribe(topics);
    }

    public void RemoveTriggers(IEnumerable<string> triggerIds)
    {
        var triggerIdList = triggerIds.ToList();
        TriggerBindings.RemoveWhere(x => triggerIdList.Contains(x.Key));
    }

    public void RemoveBookmarks(IEnumerable<string> bookmarkIds)
    {
        var bookmarkIdList = bookmarkIds.ToList();
        BookmarkBindings.RemoveWhere(x => bookmarkIdList.Contains(x.Key));
    }

    public void Subscribe(IEnumerable<BookmarkBinding> bookmarkBindings)
    {
        var topics = bookmarkBindings.SelectMany(x => x.Stimulus.Topics).Distinct().ToList();
        Subscribe(topics);
    }

    public void Dispose()
    {
        Stop();
    }
    
    private void Subscribe(IEnumerable<string> topics)
    {
        if(!_running)
            throw new InvalidOperationException("The worker is not running.");
        
        var topicList = topics.ToHashSet();
        
        // Check if there are any topics not yet in the list of subscribed topics.
        if (topicList.All(topic => _subscribedTopics.Contains(topic)))
            return;

        // Add the new topics to the list of subscribed topics.
        _subscribedTopics.UnionWith(topicList);
        
        // Update the consumer's subscription.
        consumer.Subscribe(_subscribedTopics);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(cancellationToken);

            if (consumeResult.IsPartitionEOF)
                continue;

            await ProcessMessageAsync(consumeResult.Message, cancellationToken);
        }

        consumer.Unsubscribe();
        consumer.Close();
        consumer.Dispose();
    }

    private async Task ProcessMessageAsync(Message<TKey, TValue> message, CancellationToken cancellationToken)
    {
        var headers = message.Headers.ToDictionary(x => x.Key, x => x.GetValueBytes());
        var notification = new TransportMessageReceived(this, new KafkaTransportMessage(message.Key, message.Value, headers));
        await using var scope = workerContext.ScopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.SendAsync(notification, cancellationToken);
    }
}