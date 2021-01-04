using Elsa.Persistence.DocumentDb.Documents;

namespace Elsa.Persistence.DocumentDb
{
    public interface ITenantProvider
    {
        string GetTenantId<T>() where T : DocumentBase;
    }
}