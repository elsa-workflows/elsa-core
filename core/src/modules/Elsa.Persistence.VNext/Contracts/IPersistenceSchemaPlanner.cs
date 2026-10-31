namespace Elsa.Persistence.VNext.Contracts;

public interface IPersistenceSchemaPlanner<out TPlan>
{
    TPlan Plan(PersistenceSchema schema);
}
