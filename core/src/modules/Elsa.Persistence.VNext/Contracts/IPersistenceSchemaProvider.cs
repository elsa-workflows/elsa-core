namespace Elsa.Persistence.VNext.Contracts;

public interface IPersistenceSchemaProvider
{
    PersistenceSchema DescribeSchema();
}
