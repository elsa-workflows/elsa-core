using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Elsa.Workflows.Management.Providers;

/// <summary>
/// Provides activity descriptors based on a list of NON-ELSA activity types finded into the same directory of the assembly.
/// </summary>
public class AssemblyPathActivityProvider : IActivityProvider
{
    private readonly IActivityDescriber _activityDescriber;

    public AssemblyPathActivityProvider(IActivityDescriber activityDescriber)
    {
        _activityDescriber = activityDescriber;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var activityTypes = GetActivityTypes();
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

    private IEnumerable<Type> GetActivityTypes()
    {
        var currentAssembly = Assembly.GetExecutingAssembly();

        // Get the directory of the current assembly
        var path = Path.GetDirectoryName(currentAssembly.Location);

        // Get all the assembly files in the directory
        var assemblyFiles = System.IO.Directory.GetFiles(path, "*.dll")
            .Where(f => !f.Split('\\').ToList().Last().StartsWith("Elsa"));
        List<Type> types = new List<Type>();
        foreach (var file in assemblyFiles)
        {
            // Load the assembly from the file
            var assembly = Assembly.LoadFrom(file);

            // Get all the non-abstract, non-interface and non -generic types from the assembly that implement IActivity
            var assemblyTypes = assembly.GetTypes()
                .Where(x => typeof(IActivity).IsAssignableFrom(x) && x is { IsAbstract: false, IsInterface: false, IsGenericType: false })
                .ToList();
            // Add the types to the list
            types.AddRange(assemblyTypes);
        }
        types = types.Distinct().ToList();
        return types;
    }
}
