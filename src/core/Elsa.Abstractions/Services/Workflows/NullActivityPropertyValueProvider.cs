using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class NullActivityPropertyValueProvider : IActivityPropertyValueProvider
    {
        public static readonly NullActivityPropertyValueProvider Instance = new();
        public string RawValue => "null";
        public ValueTask<object?> GetValueAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default) => new(null);
    }
}