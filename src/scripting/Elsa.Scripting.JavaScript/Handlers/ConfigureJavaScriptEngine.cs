using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Providers.WorkflowStorage;
using Elsa.Scripting.JavaScript.Events;
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
    public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScriptExpression>, INotificationHandler<RenderingTypeScriptDefinitions>
    {
        private readonly IConfiguration _configuration;
        private readonly IActivityTypeService _activityTypeService;

        public ConfigureJavaScriptEngine(IConfiguration configuration, IActivityTypeService activityTypeService)
        {
            _configuration = configuration;
            _activityTypeService = activityTypeService;
        }

        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var activityExecutionContext = notification.ActivityExecutionContext;
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;
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
            engine.SetValue("getActivityOutput", (Func<string, string?, object?>) ((activityId, propertyName) => GetActivityOutputAsync(activityId, propertyName, activityExecutionContext).Result));

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
            RegisterType<Instant>(engine);
            RegisterType<Duration>(engine);
            RegisterType<Period>(engine);
            RegisterType<LocalDate>(engine);
            RegisterType<LocalTime>(engine);
            RegisterType<LocalDateTime>(engine);
            RegisterType<Guid>(engine);
            RegisterType<WorkflowExecutionContext>(engine);
            RegisterType<ActivityExecutionContext>(engine);

            // Workflow variables.
            var variables = workflowExecutionContext.GetMergedVariables();

            foreach (var variable in variables.Data)
                engine.SetValue(variable.Key, variable.Value);

            // DEPRECATED: The following only works when activity state is stored as part of the workflow instance itself.
            // With the introduction of pluggable workflow state storage providers, which are asynchronous, we don't want to load all states of all activities upfront (e.g. we don't want to download files from blob storage).
            // Named activities.
            foreach (var activity in workflowBlueprint.Activities.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                var state = new Dictionary<string, object>(activityExecutionContext.GetActivityData(activity.Id));

                // Output.
                if (state.ContainsKey(ActivityOutput.PropertyName))
                {
                    var output = state.GetItem(ActivityOutput.PropertyName);
                    state["Output"] = output ?? new { };
                }

                engine.SetValue(activity.Name, state);
            }

            return Task.CompletedTask;
        }

        public async Task Handle(RenderingTypeScriptDefinitions notification, CancellationToken cancellationToken)
        {
            var output = notification.Output;

            output.AppendLine("declare function guid(): string");
            output.AppendLine("declare function parseGuid(text: string): Guid");
            output.AppendLine("declare function setVariable(name: string, value?: any): void;");
            output.AppendLine("declare function getVariable(name: string): any;");
            output.AppendLine("declare function getConfig(section: string): any;");
            output.AppendLine("declare function isNullOrWhiteSpace(text: string): boolean;");
            output.AppendLine("declare function isNullOrEmpty(text: string): boolean;");
            output.AppendLine("declare function getWorkflowDefinitionIdByName(name: string): string;");
            output.AppendLine("declare function getWorkflowDefinitionIdByTag(tag: string): string;");
            output.AppendLine("declare function getActivity(idOrName: string): any;");
            output.AppendLine("declare function getActivityOutput(activityIdOrName: string, propertyName?: string): any;");

            output.AppendLine("declare const activityExecutionContext: ActivityExecutionContext;");
            output.AppendLine("declare const workflowExecutionContext: WorkflowExecutionContext;");
            output.AppendLine("declare const workflowInstance: WorkflowInstance;");
            output.AppendLine("declare const workflowInstanceId: string;");
            output.AppendLine("declare const workflowDefinitionId: string;");
            output.AppendLine("declare const workflowDefinitionVersion: number;");
            output.AppendLine("declare const correlationId: string;");
            output.AppendLine("declare const currentCulture: CultureInfo;");

            var workflowDefinition = notification.WorkflowDefinition;

            if (workflowDefinition != null)
            {
                // Workflow Context
                var contextType = workflowDefinition.ContextOptions?.ContextType;

                if (contextType != null)
                {
                    var workflowContextTypeScriptType = notification.GetTypeScriptType(contextType);
                    output.AppendLine($"declare const workflowContext: {workflowContextTypeScriptType}");
                }

                // Workflow Variables.
                foreach (var variable in workflowDefinition.Variables!.Data)
                {
                    var variableType = variable.Value?.GetType() ?? typeof(object);
                    var typeScriptType = notification.GetTypeScriptType(variableType);
                    output.AppendLine($"declare const {variable.Key}: {typeScriptType}");
                }

                // Named Activities.
                var namedActivities = workflowDefinition.Activities.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();
                var activityTypeNames = namedActivities.Select(x => x.Type).Distinct().ToList();
                var activityTypes = await Task.WhenAll(activityTypeNames.Select(async activityTypeName => (activityTypeName, await _activityTypeService.GetActivityTypeAsync(activityTypeName, cancellationToken))));
                var activityTypeDictionary = activityTypes.ToDictionary(x => x.activityTypeName, x => x.Item2);

                foreach (var activityType in activityTypeDictionary.Values)
                    RenderActivityTypeDeclaration(activityType, output);

                foreach (var activity in namedActivities)
                {
                    var activityType = activityTypeDictionary[activity.Type];
                    var typeScriptType = activityType.TypeName;
                    output.AppendLine($"declare const {activity.Name}: {typeScriptType}");
                }
            }

            void RenderActivityTypeDeclaration(ActivityType type, StringBuilder writer)
            {
                var typeName = type.TypeName;
                var descriptor = type.Describe();
                var inputProperties = descriptor.InputProperties;
                var outputProperties = descriptor.OutputProperties;

                writer.AppendLine($"declare interface {typeName} {{");

                foreach (var property in inputProperties)
                {
                    var typeScriptType = notification.GetTypeScriptType(property.Type);
                    var propertyName = property.Name;
                    writer.AppendLine($"{propertyName}: {typeScriptType};");
                }

                foreach (var property in outputProperties)
                {
                    var typeScriptType = notification.GetTypeScriptType(property.Type);
                    var propertyName = property.Name;
                    writer.AppendLine($"{propertyName}: {typeScriptType};");
                }

                writer.AppendLine("}");
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

        private object? GetInboundActivityModel(ActivityExecutionContext context)
        {
            var inboundActivityId = context.WorkflowExecutionContext.GetInboundActivityPath(context.ActivityId).FirstOrDefault();
            return inboundActivityId == null ? null : GetActivityModel(context, inboundActivityId);
        }
        
        private async Task<object?> GetActivityOutputAsync(string activityIdOrName, string? propertyName, ActivityExecutionContext context)
        {
            var workflowExecutionContext = context.WorkflowExecutionContext;
            var activityBlueprint = workflowExecutionContext.GetActivityBlueprintByName(activityIdOrName) ?? workflowExecutionContext.GetActivityBlueprintById(activityIdOrName)!;
            var storageService = context.GetService<IWorkflowStorageService>();
            var propName = propertyName ?? ActivityOutput.PropertyName;
            var providerName = propertyName == null ? activityBlueprint.OutputStorageProviderName : activityBlueprint.PropertyStorageProviders.GetItem(propertyName);
            var storageContext = new WorkflowStorageContext(context.WorkflowInstance, activityBlueprint.Id);
            return await storageService.LoadAsync(providerName, storageContext, propName, context.CancellationToken);
        }

        private void RegisterType<T>(Engine engine) => engine.SetValue(typeof(T).Name, TypeReference.CreateTypeReference(engine, typeof(T)));
        private void RegisterType(Type type, Engine engine) => engine.SetValue(type.Name, TypeReference.CreateTypeReference(engine, type));
    }
}