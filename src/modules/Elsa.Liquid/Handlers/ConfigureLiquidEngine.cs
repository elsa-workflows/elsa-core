using System.Dynamic;
using Elsa.Expressions.Models;
using Elsa.Liquid.Helpers;
using Elsa.Liquid.Notifications;
using Elsa.Liquid.Options;
using Elsa.Mediator.Services;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;
using Fluid;
using Fluid.Values;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Elsa.Liquid.Handlers
{
    public class ConfigureLiquidEngine : INotificationHandler<RenderingLiquidTemplate>
    {
        private readonly IConfiguration _configuration;
        private readonly FluidOptions _fluidOptions;

        public ConfigureLiquidEngine(IConfiguration configuration, IOptions<FluidOptions> fluidOptions)
        {
            _configuration = configuration;
            _fluidOptions = fluidOptions.Value;
        }

        public Task HandleAsync(RenderingLiquidTemplate notification, CancellationToken cancellationToken)
        {
            var context = notification.TemplateContext;
            var options = context.Options;
            var memberAccessStrategy = options.MemberAccessStrategy;
            
            memberAccessStrategy.Register<ExpandoObject>();
            memberAccessStrategy.Register<LiquidPropertyAccessor, FluidValue>((x, name) => x.GetValueAsync(name));
            memberAccessStrategy.Register<ExpandoObject, object>((x, name) => ((IDictionary<string, object>)x!)[name]);
            memberAccessStrategy.Register<ExpressionExecutionContext, LiquidPropertyAccessor>("Variables", x => new LiquidPropertyAccessor(name => ToFluidValue(x.GetVariableValues(), name, options)));

            if (_fluidOptions.AllowConfigurationAccess)
            {
                memberAccessStrategy.Register<ExpressionExecutionContext, LiquidPropertyAccessor>("Configuration", x => new LiquidPropertyAccessor(name => ToFluidValue(GetConfigurationValue(name), options)));
                memberAccessStrategy.Register<ConfigurationSectionWrapper, ConfigurationSectionWrapper?>((source, name) => source.GetSection(name));
            }

            return Task.CompletedTask;
        }

        private ConfigurationSectionWrapper GetConfigurationValue(string name) => new(_configuration.GetSection(name));
        private Task<FluidValue> ToFluidValue(object? input, TemplateOptions options) => Task.FromResult(FluidValue.Create(input, options));
        private Task<FluidValue> ToFluidValue(IDictionary<string, object> dictionary, string key, TemplateOptions options) => Task.FromResult(!dictionary.ContainsKey(key) ? NilValue.Instance : FluidValue.Create(dictionary[key], options));
    }
}