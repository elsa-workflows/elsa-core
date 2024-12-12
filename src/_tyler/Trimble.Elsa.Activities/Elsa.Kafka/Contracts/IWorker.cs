namespace Elsa.Kafka;

public interface IWorker : IDisposable
{
    ConsumerDefinition ConsumerDefinition { get; }
    IDictionary<string, BookmarkBinding> BookmarkBindings { get;  }
    IDictionary<string, TriggerBinding> TriggerBindings { get; }
    void Start(CancellationToken cancellationToken);
    void Stop();
    void BindTrigger(TriggerBinding binding);
    void BindBookmark(BookmarkBinding binding);
    void RemoveTriggers(IEnumerable<string> triggerIds);
    void RemoveBookmarks(IEnumerable<string> bookmarkIds);
}