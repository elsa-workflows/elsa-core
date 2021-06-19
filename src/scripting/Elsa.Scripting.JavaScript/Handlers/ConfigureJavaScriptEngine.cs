using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Providers.WorkflowStorage;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;
using Jint;
using Jint.Runtime.Interop;
using MediatR;
using Microsoft.Extensions.Configuration;
using NodaTime;

namespace Elsa.Scripting.JavaScript.Handlers
{
    public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        private readonly IConfiguration _configuration;
        private readonly IActivityTypeService _activityTypeService;
        private readonly IWorkflowStorageService _workflowStorageService;

        public ConfigureJavaScriptEngine(IConfiguration configuration, IActivityTypeService activityTypeService, IWorkflowStorageService workflowStorageService)
        {
            _configuration = configuration;
            _activityTypeService = activityTypeService;
            _workflowStorageService = workflowStorageService;
        }

        public async Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var activityExecutionContext = notification.ActivityExecutionContext;
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            var workflowInstance = workflowExecutionContext.WorkflowInstance;
            var engine = notification.Engine;

            // Global functions.
            engine.SetValue("guid", (Func<string>) (() => Guid.NewGuid().ToString()));
            engine.SetValue("parseGuid", (Func<string, Guid>) (Guid.Parse));
            engine.SetValue("setVariable", (Action<string, object>) ((name, value) => activityExecutionContext.SetVariable(name, value)));
            engine.SetValue("getVariable", (Func<string, object?>) (name => activityExecutionContext.GetVariable(name)));
            engine.SetValue("getConfig", (Func<string, object?>) (name => _configuration.GetSection(name).Value));
            engine.SetValue("isNullOrWhiteSpace", (Func<string, bool>) (string.IsNullOrWhiteSpace));
            engine.SetValue("isNullOrEmpty", (Func<string, bool>) (string.IsNullOrEmpty));
            engine.SetValue("getWorkflowDefinitionIdByName", (Func<string, string?>) (name => GetWorkflowDefinitionIdByName(activityExecutionContext, name)));
            engine.SetValue("getWorkflowDefinitionIdByTag", (Func<string, string?>) (tag => GetWorkflowDefinitionIdByTag(activityExecutionContext, tag)));
            engine.SetValue("getActivity", (Func<string, object?>) (idOrName => GetActivityModel(activityExecutionContext, idOrName)));
            
            // Using .Result because Jint doesn't support Task-based functions yet.  
            engine.SetValue("getActivityProperty", (Func<string, string?, object?>) ((activityId, propertyName) => GetActivityPropertyAsync(activityId, propertyName, activityExecutionContext).Result));

            // Global variables.
            engine.SetValue("activityExecutionContext", activityExecutionContext);
            engine.SetValue("workflowExecutionContext", workflowExecutionContext);
            engine.SetValue("workflowInstance", workflowInstance);
            engine.SetValue("input", activityExecutionContext.Input);
            engine.SetValue("workflowInstanceId", workflowInstance.Id);
            engine.SetValue("workflowDefinitionId", workflowInstance.DefinitionId);
            engine.SetValue("workflowDefinitionVersion", workflowInstance.Version);
            engine.SetValue("correlationId", workflowInstance.CorrelationId);
            engine.SetValue("currentCulture", CultureInfo.InvariantCulture);
            engine.SetValue("workflowContext", activityExecutionContext.GetWorkflowContext());

            // Types.
            engine.RegisterType<Instant>();
            engine.RegisterType<Duration>();
            engine.RegisterType<Period>();
            engine.RegisterType<LocalDate>();
            engine.RegisterType<LocalTime>();
            engine.RegisterType<LocalDateTime>();
            engine.RegisterType<Guid>();
            engine.RegisterType<WorkflowExecutionContext>();
            engine.RegisterType<ActivityExecutionContext>();

            // Workflow variables.
            var variables = workflowExecutionContext.GetMergedVariables();

            foreach (var variable in variables.Data)
                engine.SetValue(variable.Key, variable.Value);


            // DEPRECATED: The following only works when activity state is stored as part of the workflow instance itself.
            // With the introduction of pluggable workflow state storage providers, which are asynchronous, we don't want to load all states of all activities upfront (e.g. we don't want to download files from blob storage).
            AddActivityOutputOld(engine, activityExecutionContext);

            await AddActivityOutputAsync(engine, activityExecutionContext, cancellationToken);
        }
        
        private async Task AddActivityOutputAsync(Engine engine, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            var workflowInstance = activityExecutionContext.WorkflowInstance;
            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;
            var activities = new Dictionary<string, object>();

            foreach (var activity in workflowBlueprint.Activities.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                var activityType = await _activityTypeService.GetActivityTypeAsync(activity.Type, cancellationToken);
                var activityDescriptor = await _activityTypeService.DescribeActivityType(activityType, cancellationToken);
                var outputProperties = activityDescriptor.OutputProperties;
                var storageProviderLookup = activity.PropertyStorageProviders;
                var activityModel = new Dictionary<string, object?>();
                var storageContext = new WorkflowStorageContext(workflowInstance, activity.Id);

                foreach (var property in outputProperties)
                {
                    var propertyName = property.Name;
                    var storageProviderName = storageProviderLookup.GetItem(propertyName);

                    activityModel[propertyName] = (Func<object?>) (() => _workflowStorageService.LoadAsync(storageProviderName, storageContext, propertyName, cancellationToken).Result);
                }

                activities[activity.Name!] = activityModel;
            }
            
            engine.SetValue("activities", activities);
        }

        private void AddActivityOutputOld(Engine engine, ActivityExecutionContext activityExecutionContext)
        {
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;
            
            foreach (var activity in workflowBlueprint.Activities.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                var state = new Dictionary<string, object>(activityExecutionContext.GetActivityData(activity.Id));
                engine.SetValue(activity.Name, state);
            }
        }

        private string? GetWorkflowDefinitionIdByTag(ActivityExecutionContext activityExecutionContext, string tag) => GetWorkflowDefinitionId(activityExecutionContext, x => string.Equals(x.Tag, tag, StringComparison.OrdinalIgnoreCase));
        private string? GetWorkflowDefinitionIdByName(ActivityExecutionContext activityExecutionContext, string name) => GetWorkflowDefinitionId(activityExecutionContext, x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        private string? GetWorkflowDefinitionId(ActivityExecutionContext activityExecutionContext, Func<IWorkflowBlueprint, bool> filter)
        {
            var workflowRegistry = activityExecutionContext.GetService<IWorkflowRegistry>();
            var workflowBlueprint = workflowRegistry.FindAsync(filter).Result;
            return workflowBlueprint?.Id;
        }

        private object? GetActivityModel(ActivityExecutionContext context, string idOrName)
        {
            var workflowExecutionContext = context.WorkflowExecutionContext;
            var activity = workflowExecutionContext.GetActivityBlueprintByName(idOrName) ?? workflowExecutionContext.GetActivityBlueprintById(idOrName);
            return activity == null ? null : workflowExecutionContext.WorkflowInstance.ActivityData[activity.Id];
        }
        
        private async Task<object?> GetActivityPropertyAsync(string activityIdOrName, string propertyName, ActivityExecutionContext context)
        {
            var workflowExecutionContext = context.WorkflowExecutionContext;
            var activityBlueprint = workflowExecutionContext.GetActivityBlueprintByName(activityIdOrName) ?? workflowExecutionContext.GetActivityBlueprintById(activityIdOrName)!;
            var storageService = context.GetService<IWorkflowStorageService>();
            var providerName = activityBlueprint.PropertyStorageProviders.GetItem(propertyName);
            var storageContext = new WorkflowStorageContext(context.WorkflowInstance, activityBlueprint.Id);
            return await storageService.LoadAsync(providerName, storageContext, propertyName, context.CancellationToken);
        }
    }
}