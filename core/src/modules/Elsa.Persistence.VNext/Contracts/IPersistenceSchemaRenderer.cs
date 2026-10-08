namespace Elsa.Persistence.VNext.Contracts;

public interface IPersistenceSchemaRenderer<in TPlan>
{
    IReadOnlyList<string> Render(TPlan plan);
}
