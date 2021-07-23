using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityPropertyValueProvider
    {
        public string? RawValue { get; }
        ValueTask<object?> GetValueAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default);
    }
}