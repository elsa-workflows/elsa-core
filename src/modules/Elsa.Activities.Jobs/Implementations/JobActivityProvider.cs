using Elsa.Activities.Jobs.Activities;
using Elsa.Activities.Jobs.Services;
using Elsa.Jobs.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Services;

namespace Elsa.Activities.Jobs.Implementations;

/// <summary>
/// Provides activity descriptors based on registered <see cref="IJob"/> implementations. 
/// </summary>
public class JobActivityProvider : IActivityProvider
{
    private readonly IActivityFactory _activityFactory;
    private readonly IJobRegistry _jobRegistry;

    public JobActivityProvider(IActivityFactory activityFactory, IJobRegistry jobRegistry)
    {
        _activityFactory = activityFactory;
        _jobRegistry = jobRegistry;
    }

    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var jobTypes = _jobRegistry.List();
        var descriptors = CreateDescriptors(jobTypes).ToList();
        return descriptors;
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(IEnumerable<Type> jobTypes) => jobTypes.Select(CreateDescriptor);

    private ActivityDescriptor CreateDescriptor(Type jobType)
    {
        var typeName = jobType.Name;

        return new()
        {
            ActivityType = typeName,
            DisplayName = jobType.Name,
            Description = "",
            Category = "Jobs",
            Kind = ActivityKind.Job,
            IsBrowsable = true,
            Constructor = context =>
            {
                var activity = _activityFactory.Create<JobActivity>(context);
                activity.TypeName = typeName;
                activity.JobType = jobType;
                
                return activity;
            }
        };
    }
}