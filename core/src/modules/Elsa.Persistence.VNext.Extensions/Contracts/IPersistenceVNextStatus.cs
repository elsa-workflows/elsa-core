namespace Elsa.Persistence.VNext.Extensions.Contracts;

public interface IPersistenceVNextStatus
{
    PersistenceVNextStatusSnapshot Snapshot { get; }
    void RecordSuccess(PersistenceVNextStatusSnapshot snapshot);
    void RecordFailure(PersistenceVNextStatusSnapshot snapshot);
}
