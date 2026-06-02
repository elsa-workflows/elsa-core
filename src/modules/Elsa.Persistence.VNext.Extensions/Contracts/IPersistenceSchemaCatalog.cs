using Elsa.Persistence.VNext.Contracts;

namespace Elsa.Persistence.VNext.Extensions.Contracts;

public interface IPersistenceSchemaCatalog
{
    IReadOnlyList<IPersistenceSchemaProvider> Providers { get; }
    IReadOnlyList<PersistenceSchema> Schemas { get; }
    PersistenceSchema DescribeSchema();
}
