using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.Helpers;
using Humanizer;

namespace Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;

/// <summary>
/// Provides activity descriptors based on <see cref="WorkflowDefinition"/>s stored in the database. 
/// </summary>
public class WorkflowDefinitionActivityProvider(IWorkflowDefinitionStore store, IActivityFactory activityFactory, ActivityWriter activityWriter) : IActivityProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter
        {
            UsableAsActivity = true,
            VersionOptions = VersionOptions.All
        };

        var allDescriptors = new List<ActivityDescriptor>();
        var currentPage = 0;
        const int pageSize = 100;

        while (true)
        {
            var pageArgs = PageArgs.FromPage(currentPage++, pageSize);
            var pageOfDefinitions = await store.FindManyAsync(filter, pageArgs, cancellationToken);
            var descriptors = CreateDescriptors(pageOfDefinitions.Items).ToList();

            allDescriptors.AddRange(descriptors);

            if (allDescriptors.Count >= pageOfDefinitions.TotalCount)
                break;
        }

        return allDescriptors;
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(ICollection<WorkflowDefinition> definitions)
    {
        return definitions.Select(x => CreateDescriptor(x, definitions));
    }

    private ActivityDescriptor CreateDescriptor(WorkflowDefinition definition, ICollection<WorkflowDefinition> allDefinitions)
    {
        var typeName = definition.Name!.Pascalize();

        var latestPublishedVersion = allDefinitions
            .Where(x => x.DefinitionId == definition.DefinitionId && x.IsPublished)
            .MaxBy(x => x.Version);

        var ports = definition.Outcomes.Select(outcome => new Port
        {
            Name = outcome,
            DisplayName = outcome,
            IsBrowsable = true,
            Type = PortType.Flow
        }).ToList();

        var rootPort = new Port
        {
            Name = nameof(WorkflowDefinitionActivity.Root),
            DisplayName = "Root",
            IsBrowsable = false,
            Type = PortType.Embedded
        };

        ports.Insert(0, rootPort);

        return new ActivityDescriptor
        {
            TypeName = typeName,
            Name = typeName,
            Version = definition.Version,
            DisplayName = definition.Name,
            Description = definition.Description,
            Category = definition.Options.ActivityCategory ?? "Workflows",
            Kind = ActivityKind.Action,
            IsBrowsable = definition.IsPublished,
            Inputs = DescribeInputs(definition).ToList(),
            Outputs = DescribeOutputs(definition).ToList(),
            Ports = ports,
            CustomProperties =
            {
                ["RootType"] = nameof(WorkflowDefinitionActivity),
                ["WorkflowDefinitionId"] = definition.DefinitionId,
                ["WorkflowDefinitionVersionId"] = definition.Id
            },
            ConstructionProperties = new Dictionary<string, object>
            {
                [nameof(WorkflowDefinitionActivity.WorkflowDefinitionId)] = definition.DefinitionId,
                [nameof(WorkflowDefinitionActivity.WorkflowDefinitionVersionId)] = definition.Id,
                [nameof(WorkflowDefinitionActivity.Version)] = definition.Version,
            },
            Constructor = context =>
            {
                var activity = (WorkflowDefinitionActivity)activityFactory.Create(typeof(WorkflowDefinitionActivity), context);
                activity.Type = typeName;
                activity.WorkflowDefinitionId = definition.DefinitionId;
                activity.WorkflowDefinitionVersionId = definition.Id;
                activity.Version = definition.Version;
                activity.LatestAvailablePublishedVersion = latestPublishedVersion?.Version ?? 0;
                activity.LatestAvailablePublishedVersionId = latestPublishedVersion?.Id;

                return activity;
            },
            ConfigureSerializerOptions = options =>
            {
                options.Converters.Add(new JsonIgnoreCompositeRootConverterFactory(activityWriter));
                return options;
            }
        };
    }

    private static IEnumerable<InputDescriptor> DescribeInputs(WorkflowDefinition definition)
    {
        var inputs = definition.Inputs.Select(inputDefinition =>
        {
            var nakedType = inputDefinition.Type;
            var inputName = inputDefinition.Name;
            var safeInputName = PropertyNameHelper.GetSafePropertyName(typeof(WorkflowDefinitionActivity), inputName);

            return new InputDescriptor
            {
                Type = nakedType,
                IsWrapped = true,
                ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(safeInputName),
                ValueSetter = (activity, value) => activity.SyntheticProperties[safeInputName] = value!,
                Name = safeInputName,
                DisplayName = inputDefinition.DisplayName,
                Description = inputDefinition.Description,
                Category = inputDefinition.Category,
                UIHint = inputDefinition.UIHint,
                StorageDriverType = inputDefinition.StorageDriverType,
                IsSynthetic = true
            };
        });

        foreach (var input in inputs)
            yield return input;
    }

    private static IEnumerable<OutputDescriptor> DescribeOutputs(WorkflowDefinition definition)
    {
        return definition.Outputs.Select(outputDefinition =>
        {
            var nakedType = outputDefinition.Type;
            var outputName = outputDefinition.Name;
            var safeOutputName = PropertyNameHelper.GetSafePropertyName(typeof(WorkflowDefinitionActivity), outputName);

            return new OutputDescriptor
            {
                Type = nakedType,
                ValueGetter = activity => activity.SyntheticProperties.GetValueOrDefault(safeOutputName),
                ValueSetter = (activity, value) => activity.SyntheticProperties[safeOutputName] = value!,
                Name = safeOutputName,
                DisplayName = outputDefinition.DisplayName,
                Description = outputDefinition.Description,
                IsSynthetic = true
            };
        });
    }
}