using System.Threading.Tasks;

namespace Elsa.Persistence.YesSql.Schema
{
    public interface ISchemaVersionStore
    {
        Task<int> GetVersionAsync();
        Task SaveVersionAsync(int version);
    }
}