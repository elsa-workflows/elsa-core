using Elsa.Activities.Jobs.Services;

namespace Elsa.Activities.Jobs.Implementations;

public class JobRegistry : IJobRegistry
{
    private readonly HashSet<Type> _jobTypes = new(); 
    
    public void Add(Type jobType)
    {
        _jobTypes.Add(jobType);
    }

    public IEnumerable<Type> List() => _jobTypes.ToList();
}