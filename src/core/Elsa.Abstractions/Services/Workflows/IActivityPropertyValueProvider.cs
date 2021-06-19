using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityPropertyValueProvider
    {
        ValueTask<object?> GetValueAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default);
    }
}