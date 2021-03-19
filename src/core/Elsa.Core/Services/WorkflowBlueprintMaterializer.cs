﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityProviders;
using Elsa.Models;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using NetBox.Extensions;

namespace Elsa.Services
{
    public class WorkflowBlueprintMaterializer : IWorkflowBlueprintMaterializer
    {
        private readonly IActivityTypeService _activityTypeService;
        private readonly ILogger _logger;

        public WorkflowBlueprintMaterializer(IActivityTypeService activityTypeService, ILogger<WorkflowBlueprintMaterializer> logger)
        {
            _activityTypeService = activityTypeService;
            _logger = logger;
        }
        
        public async Task<IWorkflowBlueprint> CreateWorkflowBlueprintAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken)
        {
            var manyActivityBlueprints = await Task.WhenAll(workflowDefinition.Activities.Select(async x => await CreateBlueprintsAsync(x, cancellationToken)));
            var activityBlueprints = manyActivityBlueprints.SelectMany(x => x).Distinct().ToDictionary(x => x.Id);
            var compositeActivityBlueprints = activityBlueprints.Values.Where(x => x is ICompositeActivityBlueprint).Cast<ICompositeActivityBlueprint>().ToList(); 
            var connections = compositeActivityBlueprints.SelectMany(x => x.Connections).Distinct().ToList();
            var propertyProviders = compositeActivityBlueprints.SelectMany(x => x.ActivityPropertyProviders).ToList();
            
            connections.AddRange(workflowDefinition.Connections.Select(x => ResolveConnection(x, activityBlueprints)).Where(x => x != null).Select(x => x!));
            propertyProviders.AddRange(await CreatePropertyProviders(workflowDefinition, cancellationToken));

            return new WorkflowBlueprint(
                workflowDefinition.DefinitionId,
                workflowDefinition.Version,
                workflowDefinition.TenantId,
                workflowDefinition.IsSingleton,
                workflowDefinition.Name,
                workflowDefinition.DisplayName,
                workflowDefinition.Description,
                workflowDefinition.IsLatest,
                workflowDefinition.IsPublished,
                workflowDefinition.Variables,
                workflowDefinition.CustomAttributes,
                workflowDefinition.ContextOptions,
                workflowDefinition.PersistenceBehavior,
                workflowDefinition.DeleteCompletedInstances,
                activityBlueprints.Values,
                connections,
                new ActivityPropertyProviders(propertyProviders.ToDictionary(x => x.Key, x => x.Value))
            );
        }

        private async Task<ActivityPropertyProviders> CreatePropertyProviders(ICompositeActivityDefinition compositeActivityDefinition, CancellationToken cancellationToken)
        {
            var propertyProviders = new ActivityPropertyProviders();
            var activityDefinitions = compositeActivityDefinition.Activities;

            foreach (var activityDefinition in activityDefinitions)
            {
                var activityType = await _activityTypeService.GetActivityTypeAsync(activityDefinition.Type, cancellationToken);
                var type = activityType.Type;
                var props = type.GetProperties();
                
                foreach (var property in activityDefinition.Properties)
                {
                    var prop = props.FirstOrDefault(x => x.Name == property.Name);

                    if (prop == null)
                    {
                        _logger.LogWarning("Could not find the specified property '{PropertyName}' for activity type {ActivityTypeName}. Was the activity property renamed/removed/refactored after the workflow definition was created?", property.Name, activityType.Type.Name);
                        continue;
                    }
                    
                    var provider = new ExpressionActivityPropertyValueProvider(property.Expression, property.Syntax, prop.PropertyType);
                    propertyProviders.AddProvider(activityDefinition.ActivityId, property.Name, provider);
                }
            }

            return propertyProviders;
        }

        private static IConnection? ResolveConnection(
            ConnectionDefinition connectionDefinition,
            IReadOnlyDictionary<string, IActivityBlueprint> activityDictionary)
        {
            var sourceActivityId = connectionDefinition.SourceActivityId;
            var targetActivityId = connectionDefinition.TargetActivityId;
            var source = sourceActivityId != null ? activityDictionary.GetValueOrDefault(sourceActivityId) : default;
            var target = targetActivityId != null ? activityDictionary.GetValueOrDefault(targetActivityId) : default;
            var outcome = connectionDefinition.Outcome;

            if (source == null || target == null)
                return default;

            return new Connection(source, target, outcome!);
        }

        private async Task<IEnumerable<IActivityBlueprint>> CreateBlueprintsAsync(ActivityDefinition activityDefinition, CancellationToken cancellationToken)
        {
            var list = new List<IActivityBlueprint>();
            
            if (activityDefinition is CompositeActivityDefinition compositeActivityDefinition)
            {
                var manyActivityBlueprints = await Task.WhenAll(compositeActivityDefinition.Activities.Select(async x => await CreateBlueprintsAsync(x, cancellationToken)));
                var activityBlueprints = manyActivityBlueprints.SelectMany(x => x).ToDictionary(x => x.Id);
                
                list.AddRange(activityBlueprints.Values);

                var compositeActivityBlueprint = new CompositeActivityBlueprint
                {
                    Id = activityDefinition.ActivityId,
                    Type = activityDefinition.Type,
                    Activities = activityBlueprints.Values,
                    Connections = compositeActivityDefinition.Connections.Select(x => ResolveConnection(x, activityBlueprints)).Where(x => x != null).Select(x => x!).ToList(),
                    Name = activityDefinition.Name,
                    PersistOutput = activityDefinition.PersistOutput,
                    PersistWorkflow = activityDefinition.PersistWorkflow,
                    LoadWorkflowContext = activityDefinition.LoadWorkflowContext,
                    SaveWorkflowContext = activityDefinition.SaveWorkflowContext,
                    ActivityPropertyProviders = await CreatePropertyProviders(compositeActivityDefinition, cancellationToken)
                }; 
                
                list.Add(compositeActivityBlueprint);
                
                // Connect the composite activity to its starting activities.
                var startActivities = compositeActivityBlueprint.GetStartActivities().ToList();
                compositeActivityBlueprint.Connections.AddRange(startActivities.Select(x => new Connection(compositeActivityBlueprint, x, CompositeActivity.Enter)));
            }
            else
            {
                list.Add(new ActivityBlueprint
                {
                    Id = activityDefinition.ActivityId,
                    Type = activityDefinition.Type,
                    Name = activityDefinition.Name,
                    PersistOutput = activityDefinition.PersistOutput,
                    PersistWorkflow = activityDefinition.PersistWorkflow,
                    LoadWorkflowContext = activityDefinition.LoadWorkflowContext,
                    SaveWorkflowContext = activityDefinition.SaveWorkflowContext,
                });    
            }

            return list;
        }
    }
}