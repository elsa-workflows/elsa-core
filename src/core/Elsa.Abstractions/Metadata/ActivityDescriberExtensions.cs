using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Metadata
{
    public static class ActivityDescriberExtensions
    {
        public static async Task<ActivityDescriptor> DescribeAsync<T>(this IDescribesActivityType type, CancellationToken cancellationToken = default) where T : IActivity => (await type.DescribeAsync(typeof(T), cancellationToken))!;
    }
}