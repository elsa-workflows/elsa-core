namespace Elsa.Server.GraphQL.Services
{
    public interface IMutationProvider
    {
        void Setup(ElsaMutation mutation);
    }
}