using Elsa.Workflows.Core.Activities.Flowchart.Models;

namespace Elsa.Workflows.Core.Activities.Flowchart.Serialization;

/// <summary>
/// Compares two <see cref="Connection"/> instances.
/// </summary>
public class ConnectionComparer : IEqualityComparer<Connection>
{
    /// <inheritdoc />
    public bool Equals(Connection? x, Connection? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.Source.Activity.Id.Equals(y.Source.Activity.Id) && x.Target.Activity.Id.Equals(y.Target.Activity.Id) && x.Source.Port == y.Source.Port && x.Target.Port == y.Target.Port;
    }

    /// <inheritdoc />
    public int GetHashCode(Connection obj)
    {
        // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        // Justification: These can be null when the designer is in an invalid state. For example, if it used a NotFoundActivity that no longer has the same outcomes.
        return HashCode.Combine(obj.Source?.Activity?.Id, obj.Target?.Activity?.Id, obj.Source?.Port, obj.Target?.Port);
        // ReSharper restore ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
    }
}