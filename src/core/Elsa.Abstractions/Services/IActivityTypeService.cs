using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Metadata;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityTypeService
    {
        ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken = default);
        ValueTask<ActivityType> GetActivityTypeAsync(string type, CancellationToken cancellationToken = default);
        ValueTask<RuntimeActivityInstance> ActivateActivityAsync(IActivityBlueprint activityBlueprint, CancellationToken cancellationToken = default);
        ValueTask<ActivityDescriptor> DescribeActivityType(ActivityType activityType, CancellationToken cancellationToken = default);
    }
}