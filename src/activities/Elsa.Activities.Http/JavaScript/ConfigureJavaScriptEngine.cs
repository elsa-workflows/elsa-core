using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Services;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Events;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using NJsonSchema;
using NJsonSchema.CodeGeneration.TypeScript;

namespace Elsa.Activities.Http.JavaScript
{
    public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScriptExpression>, INotificationHandler<RenderingTypeScriptDefinitions>
    {
        private readonly IAbsoluteUrlProvider _absoluteUrlProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IActivityTypeService _activityTypeService;

        public ConfigureJavaScriptEngine(
            IAbsoluteUrlProvider absoluteUrlProvider,
            IHttpContextAccessor httpContextAccessor,
            IActivityTypeService activityTypeService)
        {
            _absoluteUrlProvider = absoluteUrlProvider;
            _httpContextAccessor = httpContextAccessor;
            _activityTypeService = activityTypeService;
        }

        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;
            var activityExecutionContext = notification.ActivityExecutionContext;

            engine.SetValue(
                "queryString",
                (Func<string, string>)(key => _httpContextAccessor.HttpContext!.Request.Query[key].ToString())
            );
            engine.SetValue(
                "absoluteUrl",
                (Func<string, string>)(url => _absoluteUrlProvider.ToAbsoluteUrl(url).ToString())
            );
            engine.SetValue(
                "signalUrl",
                (Func<string, string>)(signal => activityExecutionContext.GenerateSignalUrl(signal))
            );

            return Task.CompletedTask;
        }

        public Task Handle(RenderingTypeScriptDefinitions notification, CancellationToken cancellationToken)
        {
            var output = notification.Output;

            output.AppendLine("declare function queryString(name: string): string;");
            output.AppendLine("declare function absoluteUrl(url: string): string;");
            output.AppendLine("declare function signalUrl(signal: string): string;");

            if (notification.WorkflowDefinition != null)
            {
                var namedActivities = notification.WorkflowDefinition.Activities.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();

                foreach (var activity in namedActivities)
                {
                    AddDeclaredClasses(activity, output);
                    AddDeclaredTypes(notification, activity, notification.DeclaredTypes, cancellationToken);
                }
            }
            
            return Task.CompletedTask;
        }

        private void AddDeclaredClasses(ActivityDefinition activity, StringBuilder output)
        {
            var schemaProperty = activity.Properties.FirstOrDefault(x => x.Name == "Schema");
            if (schemaProperty == null || schemaProperty.Expressions.Count == 0) return;

            var json = schemaProperty.Expressions.FirstOrDefault().Value;
            if (json == null) return;

            if (string.IsNullOrWhiteSpace(json)) return;

            var jsonSchema = JsonSchema.FromJsonAsync(json).Result;
            var generator = new TypeScriptGenerator(jsonSchema, new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                TypeScriptVersion = 4
            });

            var file = generator.GenerateFile("Json")
                .Replace("\r\n", "\n")
                .Replace("export class", "declare class")
                .Replace("export interface", "declare interface");
            output.Append(file);
        }

        private Task AddDeclaredTypes(RenderingTypeScriptDefinitions notification, ActivityDefinition activity, ICollection<string> declaredTypes, CancellationToken cancellationToken)
        {
            var typeName = activity.Type;
            var schema = GetActivitySchema(activity);
            var targetType = GetActivityTargetType(activity);

            if (!string.IsNullOrWhiteSpace(schema))
                declaredTypes.Add($"{activity.Name}: {typeName}<{schema}>");
            else if (!string.IsNullOrWhiteSpace(targetType))
                declaredTypes.Add($"{activity.Name}: {typeName}<{targetType}>");
            declaredTypes.Add($"declare interface {typeName}<T>");

            var activityType = _activityTypeService.GetActivityTypeAsync(typeName, cancellationToken).Result;
            var descriptor = activityType.DescribeAsync().Result;
            var inputProperties = descriptor.InputProperties;
            var outputProperties = descriptor.OutputProperties;

            foreach (var property in inputProperties)
                RenderActivityProperty(notification, declaredTypes, typeName, property.Name, property.Type);

            foreach (var property in outputProperties)
                RenderActivityProperty(notification, declaredTypes, typeName, property.Name, property.Type);

            return Task.CompletedTask;
        }

        private void RenderActivityProperty(RenderingTypeScriptDefinitions notification, ICollection<string> declaredTypes, string typeName, string propertyName, Type propertyType)
        {
            var typeScriptType = notification.GetTypeScriptType(propertyType);

            if (propertyName == "Output")
                declaredTypes.Add($"{propertyName}(): {typeScriptType}<T>");
        }

        private string? GetActivitySchema(ActivityDefinition activity)
        {
            var schemaProperty = activity.Properties.FirstOrDefault(x => x.Name == "Schema");
            if (schemaProperty == null || schemaProperty.Expressions.Count == 0)
                return null;

            var json = schemaProperty.Expressions.FirstOrDefault().Value;
            if (json == null)
                return null;

            if (string.IsNullOrWhiteSpace(json))
                return null;

            return "Json";
        }

        private string? GetActivityTargetType(ActivityDefinition activity)
        {
            var targetTypeProperty = activity.Properties.FirstOrDefault(x => x.Name == "TargetType");
            if (targetTypeProperty == null) return null;

            var targetTypeValue = targetTypeProperty.Expressions.FirstOrDefault().Value;
            if (targetTypeValue == null) return null;

            var targetType = Type.GetType(targetTypeValue);
            if (targetType == null) return null;

            return targetType.Name;
        }
    }
}