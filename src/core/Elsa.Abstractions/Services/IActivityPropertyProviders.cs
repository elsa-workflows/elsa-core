using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityPropertyProviders
    {
        void AddProvider(string activityId, string propertyName, IActivityPropertyValueProvider provider);
        IActivityPropertyValueProvider? GetProvider(string activityId, string propertyName);

        ValueTask SetActivityPropertiesAsync(
            IActivity activity,
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken = default);
    }
}