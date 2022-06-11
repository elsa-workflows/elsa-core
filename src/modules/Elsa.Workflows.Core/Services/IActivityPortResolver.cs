using System.Reflection;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// A node resolver inspects a given activity and returns its outbound "ports". A port is an activity that this activity connects to.
/// Node resolvers are used to inspect a workflow structure and build a graph of nodes from it for easy node traversal.
/// </summary>
public interface IActivityPortResolver
{
    int Priority { get; }
    bool GetSupportsActivity(IActivity activity);
    IEnumerable<IActivity> GetPorts(IActivity activity);
    IEnumerable<PropertyInfo> GetPorts(Type activityType);
}