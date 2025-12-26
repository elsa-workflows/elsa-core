using System.Reflection;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management;

public interface IHostMethodActivityDescriber
{
    Task<IEnumerable<ActivityDescriptor>> DescribeAsync(string key, Type hostType, CancellationToken cancellationToken = default);
    Task<ActivityDescriptor> DescribeMethodAsync(string key, Type hostType, MethodInfo method, CancellationToken cancellationToken = default);
}
