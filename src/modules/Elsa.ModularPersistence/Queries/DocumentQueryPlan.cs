using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Diagnostics;

namespace Elsa.ModularPersistence.Queries;

/// <summary>
/// Describes the diagnostic plan for a portable document query.
/// </summary>
public sealed record DocumentQueryPlan(
    DocumentQuery Query,
    StorageUnitDescriptor? StorageUnit,
    IReadOnlyCollection<StorageIndexDescriptor> ReferencedIndexes,
    IReadOnlyCollection<StoragePlanDiagnostic> Diagnostics)
{
    public bool IsExecutable => Diagnostics.All(x => x.Severity != StoragePlanDiagnosticSeverity.Error);
}
