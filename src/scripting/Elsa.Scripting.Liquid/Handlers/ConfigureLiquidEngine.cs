﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Scripting.Liquid.Helpers;
using Elsa.Scripting.Liquid.Messages;
using Elsa.Services.Models;
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

        public ConfigureLiquidEngine(IConfiguration configuration)
        {
            _configuration = configuration;
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
            memberAccessStrategy.Register<ActivityExecutionContext, LiquidPropertyAccessor>("Activities", x => new LiquidPropertyAccessor(name => ToFluidValue(GetActivityModelAsync(x, name), options)!));
            memberAccessStrategy.Register<ActivityExecutionContext, LiquidActivityModel?>("InboundActivity", GetInboundActivityModelAsync);
            memberAccessStrategy.Register<LiquidActivityModel, object?>(GetActivityProperty);
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
        private LiquidActivityModel GetActivityModelAsync(ActivityExecutionContext context, string name) => new(context, name, null);

        private object? GetInboundActivityModelPropertyAsync(ActivityExecutionContext context, string propertyName)
        {
            var activityModel = GetInboundActivityModelAsync(context);

            if (activityModel == null)
                return null;

            return GetActivityProperty(activityModel, propertyName);
        }
        
        private LiquidActivityModel? GetInboundActivityModelAsync(ActivityExecutionContext context)
        {
            var inboundActivityId = context.WorkflowExecutionContext.GetInboundActivityPath(context.ActivityId).FirstOrDefault();

            if (inboundActivityId == null)
                return null;

            return new LiquidActivityModel(context, null, inboundActivityId);
        }

        private object? GetActivityProperty(LiquidActivityModel activityModel, string name)
        {
            var activityExecutionContext = activityModel.ActivityExecutionContext;
            
            // Deprecated.
            if (name == "Output")
            {
                var output = activityExecutionContext.GetOutputFrom(activityModel.ActivityName!);
                return output != null ? new JObject(output) : default;
            }

            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            var activityId = activityModel.ActivityId ?? workflowExecutionContext.GetActivityBlueprintByName(activityModel.ActivityName!)!.Id;
            var activityState = workflowExecutionContext.WorkflowInstance.ActivityData.GetItem(activityId, () => new JObject());
            var value = activityState.GetState<object>(name);
            return value;
        }
    }
}