namespace Elsa.Persistence.VNext.Relational;

public interface IRelationalTypeMapper
{
    string Map(PersistenceColumn column);
}
