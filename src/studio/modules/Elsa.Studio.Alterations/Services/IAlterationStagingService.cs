using Elsa.Studio.Alterations.Models;

namespace Elsa.Studio.Alterations.Services;

/// <summary>
/// In-memory staging area scoped to a single editor session. Holds the alterations the user has
/// dragged onto the diagram, plus an event so the staging panel and any per-node badges can
/// re-render when the list changes.
/// </summary>
public interface IAlterationStagingService
{
    /// <summary>Returns the currently staged alterations in insertion order.</summary>
    IReadOnlyList<StagedAlteration> Items { get; }

    /// <summary>Fires after every mutation (add/update/remove/clear).</summary>
    event Action? Changed;

    /// <summary>Adds a new staged alteration and emits <see cref="Changed"/>.</summary>
    void Add(StagedAlteration item);

    /// <summary>Replaces an existing item by id; no-op if not found.</summary>
    void Update(StagedAlteration item);

    /// <summary>Removes one item by id; no-op if not found.</summary>
    void Remove(Guid id);

    /// <summary>Discards all staged items.</summary>
    void Clear();

    /// <summary>How many staged items target the given activity id.</summary>
    int CountForActivity(string activityId);
}
