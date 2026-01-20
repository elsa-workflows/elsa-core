using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Humanizer;

namespace Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;

public class WorkflowDefinitionActivityDescriptorFactory
{
    public ActivityDescriptor CreateDescriptor(WorkflowDefinition definition, WorkflowDefinition? latestPublishedDefinition = null)
    {
        var typeName = definition.Name!.Pascalize();
        
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

        return new()
        {
            TypeName = typeName,
            ClrType = typeof(WorkflowDefinitionActivity),
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
                var activityResult = context.CreateActivity<WorkflowDefinitionActivity>();
                var activity = activityResult.Activity;
                activity.Type = typeName;
                activity.WorkflowDefinitionId = definition.DefinitionId;
                activity.WorkflowDefinitionVersionId = definition.Id;
                activity.Version = definition.Version;
                activity.LatestAvailablePublishedVersion = latestPublishedDefinition?.Version ?? definition.Version;
                activity.LatestAvailablePublishedVersionId = latestPublishedDefinition?.Id ?? definition.Id;

                return activityResult;
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