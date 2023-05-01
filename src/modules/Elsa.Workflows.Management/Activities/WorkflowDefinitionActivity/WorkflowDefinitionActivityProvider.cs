using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Notifications;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Humanizer;

namespace Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;

/// <summary>
/// Provides activity descriptors based on <see cref="WorkflowDefinition"/>s stored in the database. 
/// </summary>
public class WorkflowDefinitionActivityProvider : IActivityProvider
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IActivityFactory _activityFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowDefinitionActivityProvider(IWorkflowDefinitionStore store, IActivityFactory activityFactory)
    {
        _store = store;
        _activityFactory = activityFactory;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter
        {
            UsableAsActivity = true, 
            VersionOptions = VersionOptions.All
        };
        
        var definitions = (await _store.FindManyAsync(filter, cancellationToken)).ToList();
        var descriptors = CreateDescriptors(definitions);
        return descriptors;
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(ICollection<WorkflowDefinition> definitions) =>
        definitions.Select(x => CreateDescriptor(x, definitions));

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
            Mode = PortMode.Port
        }).ToList();
        
        var rootPort = new Port
        {
            Name = nameof(WorkflowDefinitionActivity.Root),
            DisplayName = "Root",
            IsBrowsable = false,
            Mode = PortMode.Embedded
        };
        
        ports.Insert(0, rootPort);

        return new()
        {
            TypeName = typeName,
            Version = definition.Version,
            DisplayName = definition.Name,
            Description = definition.Description,
            Category = "Workflows",
            Kind = ActivityKind.Action,
            IsBrowsable = definition.IsPublished,
            Inputs = DescribeInputs(definition).ToList(),
            Outputs = DescribeOutputs(definition).ToList(),
            Ports = ports,
            CustomProperties =
            {
                ["RootType"] = nameof(WorkflowDefinitionActivity),
                ["WorkflowDefinitionVersionId"] = definition.Id
            },
            Constructor = context =>
            {
                var activity = (WorkflowDefinitionActivity)_activityFactory.Create(typeof(WorkflowDefinitionActivity), context);
                activity.Type = typeName;
                activity.WorkflowDefinitionId = definition.DefinitionId;
                activity.WorkflowDefinitionVersionId = definition.Id;
                activity.Version = definition.Version;
                activity.LatestAvailablePublishedVersion = latestPublishedVersion?.Version ?? 0;
                activity.LatestAvailablePublishedVersionId = latestPublishedVersion?.Id;

                return activity;
            }
        };
    }

    private static IEnumerable<InputDescriptor> DescribeInputs(WorkflowDefinition definition)
    {
        var inputs = definition.Inputs.Select(inputDefinition =>
        {
            var nakedType = inputDefinition.Type;

            return new InputDescriptor
            {
                Type = nakedType,
                IsWrapped = true,
                ValueGetter = activity => ((WorkflowDefinitionActivity)activity).SyntheticProperties.GetValueOrDefault(inputDefinition.Name),
                ValueSetter = (activity, value) => ((WorkflowDefinitionActivity)activity).SyntheticProperties[inputDefinition.Name] = value!,
                Name = inputDefinition.Name,
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

    private static IEnumerable<OutputDescriptor> DescribeOutputs(WorkflowDefinition definition) =>
        definition.Outputs.Select(outputDefinition =>
        {
            var nakedType = outputDefinition.Type;

            return new OutputDescriptor
            {
                Type = nakedType,
                ValueGetter = activity => ((WorkflowDefinitionActivity)activity).SyntheticProperties.GetValueOrDefault(outputDefinition.Name),
                ValueSetter = (activity, value) => ((WorkflowDefinitionActivity)activity).SyntheticProperties[outputDefinition.Name] = value!,
                Name = outputDefinition.Name,
                DisplayName = outputDefinition.DisplayName,
                Description = outputDefinition.Description,
                IsSynthetic = true
            };
        });
}