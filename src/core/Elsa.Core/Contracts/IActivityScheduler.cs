using Elsa.Models;

namespace Elsa.Contracts;

public interface IActivityScheduler
{
    bool HasAny { get; }
    void Push(ActivityWorkItem workItem);
    ActivityWorkItem Pop();
    IEnumerable<ActivityWorkItem> List();
}