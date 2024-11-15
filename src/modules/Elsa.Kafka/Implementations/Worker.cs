using Confluent.Kafka;
using Elsa.Extensions;

namespace Elsa.Kafka.Implementations;

public class Worker(ConsumerDefinition consumerDefinition) : IWorker
{
    private bool _running;
    private IConsumer<Ignore, string> _consumer = default!;
    private CancellationTokenSource _cancellationTokenSource = new();
    private readonly HashSet<string> _subscribedTopics = new();
    
    public IDictionary<string, BookmarkBinding> BookmarkBindings { get;  } = new Dictionary<string, BookmarkBinding>();
    public IDictionary<string, TriggerBinding> TriggerBindings { get; } = new Dictionary<string, TriggerBinding>();
    public Func<Worker, Message<Ignore, string>, CancellationToken, Task>? MessageReceived { get; set; }
    public ConsumerDefinition ConsumerDefinition { get; } = consumerDefinition;

    public void Start(CancellationToken cancellationToken)
    {
        if (_running)
            return;

        _running = true;

        var config = new ConsumerConfig
        {
            BootstrapServers = string.Join(",", ConsumerDefinition.BootstrapServers),
            GroupId = ConsumerDefinition.GroupId,
            AutoOffsetReset = ConsumerDefinition.AutoOffsetReset,
            EnableAutoCommit = ConsumerDefinition.EnableAutoCommit
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = Task.Run(() => RunAsync(_cancellationTokenSource.Token), cancellationToken);
    }

    public void Stop()
    {
        if (!_running)
            return;

        _running = false;
        _consumer.Unsubscribe();
        _consumer.Close();
        _consumer.Dispose();
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
        _consumer.Subscribe(_subscribedTopics);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var consumeResult = _consumer.Consume(cancellationToken);

            if (consumeResult.IsPartitionEOF)
                continue;

            await ProcessMessageAsync(consumeResult.Message);
        }

        _consumer.Unsubscribe();
        _consumer.Close();
        _consumer.Dispose();
    }

    private Task ProcessMessageAsync(Message<Ignore, string> message)
    {
        MessageReceived?.Invoke(this, message, _cancellationTokenSource.Token);
        return Task.CompletedTask;
    }
}