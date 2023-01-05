using Proto.Persistence;

namespace Elsa.ProtoActor.Extensions;

internal static class PersistenceExtensions
{
    public static async Task<Persistence> PersistRollingEventAsync(this Persistence persistence, object @event, int eventsPerSnapshot)
    {
        await persistence.PersistEventAsync(@event);
        await CleanupHistoryAsync(persistence, eventsPerSnapshot);
        return persistence;
    }
    
    public static async Task<Persistence> PersistRollingSnapshotAsync(this Persistence persistence, object snapshot, int maxSnapshots)
    {
        await persistence.PersistSnapshotAsync(snapshot);
        
        // If we stored enough snapshots, delete the older snapshots.
        if (persistence.Index % maxSnapshots == 0)
        {
            var snapshotToDelete = persistence.Index - maxSnapshots;
            await persistence.DeleteSnapshotsAsync(snapshotToDelete);
        }
        
        return persistence;
    }
    
    private static async Task CleanupHistoryAsync(this Persistence persistence, int eventsPerSnapshot)
    {
        if (persistence.Index == 0)
            return;
        
        // If we stored enough events to create a snapshot, delete the events and the previous snapshots.
        if (persistence.Index % eventsPerSnapshot == 0)
        {
            var snapshotToDelete = persistence.Index - eventsPerSnapshot;
            await persistence.DeleteEventsAsync(persistence.Index);
            await persistence.DeleteSnapshotsAsync(snapshotToDelete);
        }
    }
}