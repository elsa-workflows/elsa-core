using System.Collections.Generic;
using System.Dynamic;
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

            options.MemberAccessStrategy.Register<ExpandoObject>();
            options.MemberAccessStrategy.Register<JObject>();
            options.MemberAccessStrategy.Register<JValue>(o => o.Value);
            options.MemberAccessStrategy.Register<LiquidActivityModel>();
            options.MemberAccessStrategy.Register<LiquidPropertyAccessor, FluidValue>((x, name) => x.GetValueAsync(name));
            options.MemberAccessStrategy.Register<ActivityExecutionContext, FluidValue>("Input", x => ToFluidValue(x.Input, options));
            options.MemberAccessStrategy.Register<ActivityExecutionContext, FluidValue>("WorkflowInstanceId", x => ToFluidValue(x.WorkflowInstance.Id, options));
            options.MemberAccessStrategy.Register<ActivityExecutionContext, FluidValue>("CorrelationId", x => ToFluidValue(x.CorrelationId, options));
            options.MemberAccessStrategy.Register<ActivityExecutionContext, FluidValue>("WorkflowDefinitionId", x => ToFluidValue(x.WorkflowInstance.DefinitionId, options));
            options.MemberAccessStrategy.Register<ActivityExecutionContext, FluidValue>("WorkflowDefinitionVersion", x => ToFluidValue(x.WorkflowInstance.Version, options));
            options.MemberAccessStrategy.Register<ActivityExecutionContext, LiquidPropertyAccessor>("Variables", x => new LiquidPropertyAccessor(name => ToFluidValue(x.WorkflowExecutionContext.GetMergedVariables(), name, options)));
            options.MemberAccessStrategy.Register<ActivityExecutionContext, LiquidPropertyAccessor>("Activities", x => new LiquidPropertyAccessor(name => ToFluidValue(GetActivityModelAsync(x, name), options)!));
            options.MemberAccessStrategy.Register<LiquidActivityModel, object?>("Output", GetActivityOutput);
            options.MemberAccessStrategy.Register<LiquidObjectAccessor<object>, object>((x, name) => x.GetValueAsync(name));
            options.MemberAccessStrategy.Register<ExpandoObject, object>((x, name) => ((IDictionary<string, object>) x)[name]);
            options.MemberAccessStrategy.Register<JObject, object?>((source, name) => source[name]);
            options.MemberAccessStrategy.Register<ActivityExecutionContext, LiquidPropertyAccessor>("Configuration", x => new LiquidPropertyAccessor(name => ToFluidValue(GetConfigurationValue(name), options)!));
            options.MemberAccessStrategy.Register<ConfigurationSectionWrapper, ConfigurationSectionWrapper?>((source, name) => source.GetSection(name));

            return Task.CompletedTask;
        }

        private ConfigurationSectionWrapper GetConfigurationValue(string name) => new(_configuration.GetSection(name));
        private Task<FluidValue> ToFluidValue(object? input, TemplateOptions options) => Task.FromResult(FluidValue.Create(input, options));
        private Task<FluidValue?> ToFluidValue(Variables dictionary, string key, TemplateOptions options) => Task.FromResult(!dictionary.Has(key) ? default : FluidValue.Create(dictionary.Get(key), options));
        private LiquidActivityModel GetActivityModelAsync(ActivityExecutionContext context, string name) => new(context, name);

        private Task<object?> GetActivityOutput(LiquidActivityModel activityModel)
        {
            var output = activityModel.ActivityExecutionContext.GetOutputFrom(activityModel.ActivityName);
            return Task.FromResult(output);
        }
    }
}