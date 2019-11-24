namespace Elsa.Server.GraphQL.Services
{
    public interface IQueryProvider
    {
        void Setup(ElsaQuery query);
    }
}