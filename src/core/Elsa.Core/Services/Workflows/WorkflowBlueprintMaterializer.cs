using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetBox.Extensions;

namespace Elsa.Services.Workflows
{
    public class WorkflowBlueprintMaterializer : IWorkflowBlueprintMaterializer
    {
        private readonly IActivityTypeService _activityTypeService;
        private readonly IGetsStartActivities _startingActivitiesProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public WorkflowBlueprintMaterializer(
            IActivityTypeService activityTypeService,
            IGetsStartActivities startingActivitiesProvider,
            IServiceProvider serviceProvider,
            ILogger<WorkflowBlueprintMaterializer> logger)
        {
            _startingActivitiesProvider = startingActivitiesProvider;
            _serviceProvider = serviceProvider;
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
                workflowDefinition.Tag,
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
                var activityDescriptor = activityType.Describe();
                var propertyDescriptors = activityDescriptor.InputProperties;

                foreach (var property in activityDefinition.Properties)
                {
                    var propertyDescriptor = propertyDescriptors.FirstOrDefault(x => x.Name == property.Name);
                    
                    if (propertyDescriptor == null)
                    {
                        _logger.LogWarning("Could not find the specified property '{PropertyName}' for activity type {ActivityTypeName}", property.Name, activityType.TypeName);
                        continue;
                    }

                    var syntax = property.Syntax ?? propertyDescriptor.DefaultSyntax ?? SyntaxNames.Literal;
                    var expression = property.GetExpression(syntax);
                    var provider = new ExpressionActivityPropertyValueProvider(expression, syntax, propertyDescriptor.Type);
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
            var activityType = await _activityTypeService.GetActivityTypeAsync(activityDefinition.Type, cancellationToken);
            
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
                    DisplayName = activityDefinition.DisplayName,
                    Description = activityDefinition.Description,
                    PersistWorkflow = activityDefinition.PersistWorkflow,
                    LoadWorkflowContext = activityDefinition.LoadWorkflowContext,
                    SaveWorkflowContext = activityDefinition.SaveWorkflowContext,
                    ActivityPropertyProviders = await CreatePropertyProviders(compositeActivityDefinition, cancellationToken),
                    PropertyStorageProviders = activityDefinition.PropertyStorageProviders
                };

                list.Add(compositeActivityBlueprint);

                // Connect the composite activity to its starting activities.
                var startActivities = _startingActivitiesProvider.GetStartActivities(compositeActivityBlueprint).ToList();
                compositeActivityBlueprint.Connections.AddRange(startActivities.Select(x => new Connection(compositeActivityBlueprint, x, CompositeActivity.Enter)));
            }
            else if (typeof(CompositeActivity).IsAssignableFrom(activityType.Type))
            {
                var compositeActivity = (CompositeActivity) ActivatorUtilities.CreateInstance(_serviceProvider, activityType.Type);
                var compositeActivityBuilder = new CompositeActivityBuilder(_serviceProvider, _startingActivitiesProvider, activityType.Type, activityType.TypeName)
                {
                    ActivityId = activityDefinition.ActivityId,
                    Name = activityDefinition.Name,
                    DisplayName = activityDefinition.DisplayName,
                    Description = activityDefinition.Description,
                    PersistWorkflowEnabled = activityDefinition.PersistWorkflow,
                    LoadWorkflowContextEnabled = activityDefinition.LoadWorkflowContext,
                    SaveWorkflowContextEnabled = activityDefinition.SaveWorkflowContext,
                    PropertyStorageProviders = activityDefinition.PropertyStorageProviders
                };
                
                compositeActivity.Build(compositeActivityBuilder);

                var compositeActivityBlueprint = compositeActivityBuilder.Build($"{activityDefinition.ActivityId}:activity");

                list.Add(compositeActivityBlueprint);
                list.AddRange(compositeActivityBlueprint.Activities);
                
                // Connect the composite activity to its starting activities.
                var startActivities = _startingActivitiesProvider.GetStartActivities(compositeActivityBlueprint).ToList();
                compositeActivityBlueprint.Connections.AddRange(startActivities.Select(x => new Connection(compositeActivityBlueprint, x, CompositeActivity.Enter)));
            }
            else
            {
                list.Add(new ActivityBlueprint
                {
                    Id = activityDefinition.ActivityId,
                    Type = activityDefinition.Type,
                    Name = activityDefinition.Name,
                    DisplayName = activityDefinition.DisplayName,
                    Description = activityDefinition.Description,
                    PersistWorkflow = activityDefinition.PersistWorkflow,
                    LoadWorkflowContext = activityDefinition.LoadWorkflowContext,
                    SaveWorkflowContext = activityDefinition.SaveWorkflowContext,
                    PropertyStorageProviders = activityDefinition.PropertyStorageProviders
                });
            }

            return list;
        }
    }
}