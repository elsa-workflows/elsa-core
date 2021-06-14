using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Providers.WorkflowStorage;
using Elsa.Scripting.Liquid.Helpers;
using Elsa.Scripting.Liquid.Messages;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;
using Fluid;
using Fluid.Values;
using MediatR;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Elsa.Scripting.Liquid.Handlers
{
    public class ConfigureLiquidEngine : INotificationHandler<EvaluatingLiquidExpression>
    {
        private readonly IConfiguration _configuration;
        private readonly IWorkflowStorageService _workflowStorageService;

        public ConfigureLiquidEngine(IConfiguration configuration, IWorkflowStorageService workflowStorageService)
        {
            _configuration = configuration;
            _workflowStorageService = workflowStorageService;
        }

        public Task Handle(EvaluatingLiquidExpression notification, CancellationToken cancellationToken)
        {
            var context = notification.TemplateContext;
            var options = context.Options;
            var memberAccessStrategy = options.MemberAccessStrategy;

            options.ValueConverters.Add(x => x is JObject o ? new ObjectValue(o) : null);
            options.ValueConverters.Add(x => x is JValue v ? v.Value : null);

            memberAccessStrategy.Register<ExpandoObject>();
            memberAccessStrategy.Register<JObject>();
            memberAccessStrategy.Register<JValue>(o => o.Value);
            memberAccessStrategy.Register<LiquidActivityModel>();
            memberAccessStrategy.Register<LiquidPropertyAccessor, FluidValue>((x, name) => x.GetValueAsync(name));
            memberAccessStrategy.Register<ActivityExecutionContext, FluidValue>("Input", x => ToFluidValue(x.Input, options));
            memberAccessStrategy.Register<ActivityExecutionContext, FluidValue>("WorkflowInstanceId", x => ToFluidValue(x.WorkflowInstance.Id, options));
            memberAccessStrategy.Register<ActivityExecutionContext, FluidValue>("CorrelationId", x => ToFluidValue(x.CorrelationId, options));
            memberAccessStrategy.Register<ActivityExecutionContext, FluidValue>("WorkflowDefinitionId", x => ToFluidValue(x.WorkflowInstance.DefinitionId, options));
            memberAccessStrategy.Register<ActivityExecutionContext, FluidValue>("WorkflowDefinitionVersion", x => ToFluidValue(x.WorkflowInstance.Version, options));
            memberAccessStrategy.Register<ActivityExecutionContext, LiquidPropertyAccessor>("Variables", x => new LiquidPropertyAccessor(name => ToFluidValue(x.WorkflowExecutionContext.GetMergedVariables(), name, options)));
            memberAccessStrategy.Register<ActivityExecutionContext, LiquidPropertyAccessor>("Activities", x => new LiquidPropertyAccessor(name => ToFluidValue(GetActivityModel(x, name), options)!));
            memberAccessStrategy.Register<ActivityExecutionContext, LiquidActivityModel>(GetActivityModel);
            memberAccessStrategy.Register<LiquidActivityModel, object?>((model, name) => GetActivityProperty(model, name, cancellationToken));
            memberAccessStrategy.Register<LiquidObjectAccessor<JObject>, JObject>((x, name) => x.GetValueAsync(name));
            memberAccessStrategy.Register<ExpandoObject, object>((x, name) => ((IDictionary<string, object>) x)[name]);
            memberAccessStrategy.Register<JObject, object?>((source, name) => source.GetValue(name, StringComparison.OrdinalIgnoreCase));
            memberAccessStrategy.Register<ActivityExecutionContext, LiquidPropertyAccessor>("Configuration", x => new LiquidPropertyAccessor(name => ToFluidValue(GetConfigurationValue(name), options)!));
            memberAccessStrategy.Register<ConfigurationSectionWrapper, ConfigurationSectionWrapper?>((source, name) => source.GetSection(name));

            return Task.CompletedTask;
        }

        private ConfigurationSectionWrapper GetConfigurationValue(string name) => new(_configuration.GetSection(name));
        private Task<FluidValue> ToFluidValue(object? input, TemplateOptions options) => Task.FromResult(FluidValue.Create(input, options));
        private Task<FluidValue> ToFluidValue(Variables dictionary, string key, TemplateOptions options) => Task.FromResult(!dictionary.Has(key) ? NilValue.Instance : FluidValue.Create(dictionary.Get(key), options));
        private LiquidActivityModel GetActivityModel(ActivityExecutionContext context, string name) => new(context, name, null);

        private LiquidActivityModel? GetInboundActivityModelAsync(ActivityExecutionContext context)
        {
            var inboundActivityId = context.WorkflowExecutionContext.GetInboundActivityPath(context.ActivityId).FirstOrDefault();

            if (inboundActivityId == null)
                return null;

            return new LiquidActivityModel(context, null, inboundActivityId);
        }

        private async Task<object?> GetActivityProperty(LiquidActivityModel activityModel, string name, CancellationToken cancellationToken)
        {
            var activityExecutionContext = activityModel.ActivityExecutionContext;
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            var activityBlueprint = activityModel.ActivityId != null ? workflowExecutionContext.GetActivityBlueprintById(activityModel.ActivityId)! : workflowExecutionContext.GetActivityBlueprintByName(activityModel.ActivityName!)!;
            var activityId = activityBlueprint.Id;
            var storageProviderName = activityBlueprint.PropertyStorageProviders.GetItem(name);
            var storageContext = new WorkflowStorageContext(workflowExecutionContext.WorkflowInstance, activityId);
            var value = await _workflowStorageService.LoadAsync(storageProviderName, storageContext, name, cancellationToken);
            return value;
        }
    }
}