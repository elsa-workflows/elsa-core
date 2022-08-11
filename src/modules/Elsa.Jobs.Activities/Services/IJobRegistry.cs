namespace Elsa.Jobs.Activities.Services;

/// <summary>
/// Represents a registry of jobs. 
/// </summary>
public interface IJobRegistry
{
    void Add(Type jobType);
    IEnumerable<Type> List();
}