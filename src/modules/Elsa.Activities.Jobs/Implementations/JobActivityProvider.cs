using System.ComponentModel;
using System.Reflection;
using Elsa.Activities.Jobs.Activities;
using Elsa.Activities.Jobs.Attributes;
using Elsa.Activities.Jobs.Helpers;
using Elsa.Activities.Jobs.Services;
using Elsa.Jobs.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Services;
using Humanizer;

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

    public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var jobTypes = _jobRegistry.List();
        var descriptors = CreateDescriptors(jobTypes).ToList();
        return new(descriptors);
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(IEnumerable<Type> jobTypes) => jobTypes.Select(CreateDescriptor);

    private ActivityDescriptor CreateDescriptor(Type jobType)
    {
        var jobAttr = jobType.GetCustomAttribute<JobAttribute>();
        var ns = jobAttr?.Namespace ?? JobTypeNameHelper.GenerateNamespace(jobType);
        var typeName = jobAttr?.TypeName ?? jobType.Name;
        var fullTypeName = JobTypeNameHelper.GenerateTypeName(jobType);
        var displayNameAttr = jobType.GetCustomAttribute<DisplayNameAttribute>();
        var displayName = displayNameAttr?.DisplayName ?? jobAttr?.DisplayName ?? typeName.Humanize(LetterCasing.Title);
        var categoryAttr = jobType.GetCustomAttribute<CategoryAttribute>();
        var category = categoryAttr?.Category ?? jobAttr?.Category ?? ActivityTypeNameHelper.GetCategoryFromNamespace(ns) ?? "Miscellaneous";
        var descriptionAttr = jobType.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? jobAttr?.Description;

        return new()
        {
            ActivityType = fullTypeName,
            DisplayName = displayName,
            Description = description,
            Category = category,
            Kind = ActivityKind.Job,
            IsBrowsable = true,
            Constructor = context =>
            {
                var activity = _activityFactory.Create<JobActivity>(context);
                activity.TypeName = fullTypeName;
                activity.JobType = jobType;
                
                return activity;
            }
        };
    }
}