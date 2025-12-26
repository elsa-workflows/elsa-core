using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.Activities.CodeFirst;

/// <summary>
/// Provides activities for each configured host method type registered via <see cref="HostMethodActivitiesOptions"/>.
/// Public async methods (Task or Task&lt;T&gt; where T is string, object or AgentRunResponse) are exposed as activities.
/// Inputs come from public properties and method parameters.
/// </summary>
[UsedImplicitly]
public class HostMethodActivityProvider(IOptions<HostMethodActivitiesOptions> options, IHostMethodActivityDescriber hostMethodActivityDescriber) : IActivityProvider
{
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var descriptors = new List<ActivityDescriptor>();

        foreach (var kvp in options.Value.ActivityTypes)
        {
            var key = kvp.Key;
            var type = kvp.Value;
            var methodDescriptors = await hostMethodActivityDescriber.DescribeAsync(key, type, cancellationToken);
            descriptors.AddRange(methodDescriptors);
        }

        return descriptors;
    }
}
