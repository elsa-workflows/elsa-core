using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityPropertyProviders : IEnumerable<KeyValuePair<string, IDictionary<string, IActivityPropertyValueProvider>>>
    {
        void AddProvider(string activityId, string propertyName, IActivityPropertyValueProvider provider);
        IActivityPropertyValueProvider? GetProvider(string activityId, string propertyName);
        IDictionary<string, IActivityPropertyValueProvider>? GetProviders(string activityId);

        ValueTask SetActivityPropertiesAsync(
            IActivity activity,
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken = default);
    }
}