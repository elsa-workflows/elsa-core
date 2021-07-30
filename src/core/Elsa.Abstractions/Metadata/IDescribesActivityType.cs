using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Metadata
{
    public interface IDescribesActivityType
    {
        Task<ActivityDescriptor> DescribeAsync(Type activityType, CancellationToken cancellationToken = default);
    }
}