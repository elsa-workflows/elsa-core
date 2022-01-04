using System.Runtime.CompilerServices;
using Elsa.Management.Contracts;
using Elsa.Management.Models;
using Elsa.Management.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Management.Providers;

public class TypedActivityProvider : IActivityProvider
{
    private readonly IActivityDescriber _activityDescriber;
    private readonly ApiOptions _options;

    public TypedActivityProvider(IOptions<ApiOptions> options, IActivityDescriber activityDescriber)
    {
        _activityDescriber = activityDescriber;
        _options = options.Value;
    }
        
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var activityTypes = _options.ActivityTypes;
        var descriptors = await DescribeActivityTypesAsync(activityTypes, cancellationToken).ToListAsync(cancellationToken);
        return descriptors;
    }
        
    public async IAsyncEnumerable<ActivityDescriptor> DescribeActivityTypesAsync(IEnumerable<Type> activityTypes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var activityType in activityTypes)
        {
            var descriptor = await _activityDescriber.DescribeActivityAsync(activityType, cancellationToken);
            yield return descriptor;
        }
    }
}