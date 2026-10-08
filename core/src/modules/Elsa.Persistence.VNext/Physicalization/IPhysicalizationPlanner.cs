using Elsa.Persistence.VNext;

namespace Elsa.Persistence.VNext.Physicalization;

public interface IPhysicalizationPlanner
{
    PhysicalizationPlan Plan(PersistenceSchema schema, StoragePhysicalizationPolicy policy);
}
