using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Expressions.Helpers;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Services;
using Humanizer;

namespace Elsa.Workflows.Management.Providers;

/// <summary>
/// Provides activity descriptors based on <see cref="WorkflowDefinition"/>s stored in the database. 
/// </summary>
public class WorkflowDefinitionActivityProvider : IActivityProvider
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IActivityFactory _activityFactory;
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowDefinitionActivityProvider(IWorkflowDefinitionStore store, IActivityFactory activityFactory, IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _store = store;
        _activityFactory = activityFactory;
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var definitions = (await _store.FindWithActivityBehaviorAsync(VersionOptions.All, cancellationToken)).ToList();
        var descriptors = CreateDescriptors(definitions);
        return descriptors;
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(IEnumerable<WorkflowDefinition> definitions) => definitions.Select(CreateDescriptor);

    private ActivityDescriptor CreateDescriptor(WorkflowDefinition definition)
    {
        var typeName = definition.Name!.Pascalize();

        return new()
        {
            TypeName = typeName,
            Version = definition.Version,
            DisplayName = definition.Name,
            Description = definition.Description,
            Category = "Workflows",
            Kind = ActivityKind.Action,
            IsBrowsable = definition.IsPublished,
            ActivityType = typeof(WorkflowDefinitionActivity),
            Inputs = DescribeInputs(definition).ToList(),
            Constructor = context =>
            {
                var activity = (WorkflowDefinitionActivity)_activityFactory.Create(typeof(WorkflowDefinitionActivity), context);
                activity.Type = typeName;
                activity.WorkflowDefinitionId = definition.DefinitionId;
                activity.Version = definition.Version;

                foreach (var inputDefinition in definition.Inputs)
                {
                    var inputName = inputDefinition.Name;
                    var variable = definition.Variables.FirstOrDefault(x => x.Name == inputName);
                    
                    if(variable == null)
                        continue;

                    var propertyName = inputName.ToLowerInvariant();
                    var nakedType = inputDefinition.Type;
                    var wrappedType = typeof(Input<>).MakeGenericType(nakedType);
                    
                    if (context.Element.TryGetProperty(propertyName, out var propertyElement))
                    {
                        var json = propertyElement.ToString();
                        var inputValue = JsonSerializer.Deserialize(json, wrappedType, context.SerializerOptions);
                        activity.ResolvedInputValues[variable.Name] = inputValue!;
                    }
                }
                
                return activity;
            }
        };
    }

    private IEnumerable<InputDescriptor> DescribeInputs(WorkflowDefinition definition) =>
        definition.Inputs.Select(inputDefinition =>
        {
            var nakedType = inputDefinition.Type;
            var wrappedType = typeof(Input<>).MakeGenericType(nakedType);
            
            return new InputDescriptor
            {
                Type = wrappedType,
                IsWrapped = true,
                Name = inputDefinition.Name,
                DisplayName = inputDefinition.DisplayName,
                Description = inputDefinition.Description,
                Category = inputDefinition.Category,
                UIHint = inputDefinition.UIHint
            };
        });
}