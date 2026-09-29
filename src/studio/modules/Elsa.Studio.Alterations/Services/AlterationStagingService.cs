using Elsa.Studio.Alterations.Models;

namespace Elsa.Studio.Alterations.Services;

/// <summary>
/// Default in-memory implementation. Registered scoped — one instance per Blazor circuit, which
/// is what we want: each open editor has its own staging list.
/// </summary>
public class AlterationStagingService : IAlterationStagingService
{
    private readonly List<StagedAlteration> _items = new();

    /// <inheritdoc />
    public IReadOnlyList<StagedAlteration> Items => _items;

    /// <inheritdoc />
    public event Action? Changed;

    /// <inheritdoc />
    public void Add(StagedAlteration item)
    {
        _items.Add(item);
        Changed?.Invoke();
    }

    /// <inheritdoc />
    public void Update(StagedAlteration item)
    {
        var index = _items.FindIndex(x => x.Id == item.Id);
        if (index < 0) return;
        _items[index] = item;
        Changed?.Invoke();
    }

    /// <inheritdoc />
    public void Remove(Guid id)
    {
        var index = _items.FindIndex(x => x.Id == id);
        if (index < 0) return;
        _items.RemoveAt(index);
        Changed?.Invoke();
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (_items.Count == 0) return;
        _items.Clear();
        Changed?.Invoke();
    }

    /// <inheritdoc />
    public int CountForActivity(string activityId)
        => _items.Count(x => x.TargetActivityId == activityId);
}
