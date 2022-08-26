using System.Threading.Tasks;

namespace Elsa.Activities.Sql.Contracts
{
    public interface ISqlConnectionStringHandler
    {
        Task<string> EvaluateStoredValue(string originalValue);
    }
}
