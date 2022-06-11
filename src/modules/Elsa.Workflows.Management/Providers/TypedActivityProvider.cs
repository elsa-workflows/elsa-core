using System.Runtime.CompilerServices;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.Providers;

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

    private async IAsyncEnumerable<ActivityDescriptor> DescribeActivityTypesAsync(IEnumerable<Type> activityTypes, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var activityType in activityTypes)
        {
            var descriptor = await _activityDescriber.DescribeActivityAsync(activityType, cancellationToken);
            yield return descriptor;
        }
    }
}