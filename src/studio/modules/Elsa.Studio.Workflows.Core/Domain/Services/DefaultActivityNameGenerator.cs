using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <inheritdoc />
public class DefaultActivityNameGenerator : IActivityNameGenerator
{
    /// <inheritdoc />
    public bool GetNameExists(IEnumerable<JsonObject> activities, string name)
    {
        return activities.Any(x => x.GetName() == name);
    }

    /// <inheritdoc />
    public string GenerateNextName(IEnumerable<JsonObject> activities, ActivityDescriptor activityDescriptor)
    {
        var max = 10000;
        var enumerable = activities as JsonObject[] ?? activities.ToArray();
        var count = GetNextNumber(enumerable, activityDescriptor);

        while (count++ < max)
        {
            var nextName = $"{activityDescriptor.Name}{count}";
            if (!GetNameExists(enumerable, nextName))
                return nextName;
        }

        throw new Exception("Could not generate a unique name.");
    }
    
    private static int GetNextNumber(IEnumerable<JsonObject> activities, ActivityDescriptor activityDescriptor)
    {
        return activities.Count(x => x.GetTypeName() == activityDescriptor.TypeName);
    }
}