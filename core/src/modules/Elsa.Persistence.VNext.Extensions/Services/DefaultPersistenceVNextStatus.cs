using Elsa.Persistence.VNext.Extensions.Contracts;

namespace Elsa.Persistence.VNext.Extensions.Services;

public class DefaultPersistenceVNextStatus : IPersistenceVNextStatus
{
    private PersistenceVNextStatusSnapshot _snapshot = PersistenceVNextStatusSnapshot.NotStarted;

    public PersistenceVNextStatusSnapshot Snapshot => _snapshot;

    public void RecordSuccess(PersistenceVNextStatusSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public void RecordFailure(PersistenceVNextStatusSnapshot snapshot)
    {
        _snapshot = snapshot;
    }
}
