namespace Elsa.Diagnostics.ConsoleLogs.Providers.InMemory;

public class RingBuffer<T>
{
    private readonly Queue<T> _items = new();
    private readonly object _lock = new();
    private readonly int _capacity;

    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

        _capacity = capacity;
    }

    public long DroppedCount { get; private set; }

    public void Add(T item)
    {
        lock (_lock)
        {
            if (_items.Count == _capacity)
            {
                _items.Dequeue();
                DroppedCount++;
            }

            _items.Enqueue(item);
        }
    }

    public IReadOnlyCollection<T> Snapshot()
    {
        lock (_lock)
            return _items.ToList();
    }
}
