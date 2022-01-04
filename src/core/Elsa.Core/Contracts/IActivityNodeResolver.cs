namespace Elsa.Contracts;

public interface IActivityNodeResolver
{
    int Priority { get; }
    bool GetSupportsActivity(IActivity activity);
    IEnumerable<IActivity> GetPorts(IActivity activity);
}