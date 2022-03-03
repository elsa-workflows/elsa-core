using System.Threading.Tasks;
using Elsa.Retention.Models;

namespace Elsa.Retention.Pipeline;

public interface IRetentionFilter
{
    ValueTask<bool> GetShouldDeleteAsync(CleanupContext context);
}