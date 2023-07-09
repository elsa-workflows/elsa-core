using System.Dynamic;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Liquid.Helpers;
using Elsa.Liquid.Notifications;
using Elsa.Liquid.Options;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Management.Options;
using Fluid;
using Fluid.Values;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Elsa.Liquid.Handlers;

/// <summary>
/// Configures the liquid templating engine before evaluating a liquid expression.
/// </summary>
internal class ConfigureLiquidEngine : INotificationHandler<RenderingLiquidTemplate>
{
    private readonly IConfiguration _configuration;
    private readonly ManagementOptions _managementOptions;
    private readonly FluidOptions _fluidOptions;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ConfigureLiquidEngine(IConfiguration configuration, IOptions<FluidOptions> fluidOptions, IOptions<ManagementOptions> managementOptions)
    {
        _configuration = configuration;
        _managementOptions = managementOptions.Value;
        _fluidOptions = fluidOptions.Value;
    }

    /// <inheritdoc />
    public Task HandleAsync(RenderingLiquidTemplate notification, CancellationToken cancellationToken)
    {
        var context = notification.TemplateContext;
        var options = context.Options;
        var memberAccessStrategy = options.MemberAccessStrategy;

        memberAccessStrategy.Register<ExpandoObject>();
        memberAccessStrategy.Register<LiquidPropertyAccessor, FluidValue>((x, name) => x.GetValueAsync(name));
        memberAccessStrategy.Register<ExpandoObject, object>((x, name) => ((IDictionary<string, object>)x!)[name]);
        memberAccessStrategy.Register<ExpressionExecutionContext, LiquidPropertyAccessor>("Variables", x => new LiquidPropertyAccessor(name => GetVariable(x, name, options)));
        memberAccessStrategy.Register<ExpressionExecutionContext, string?>("CorrelationId", x => x.GetWorkflowExecutionContext().CorrelationId);

        if (_fluidOptions.AllowConfigurationAccess)
        {
            memberAccessStrategy.Register<ExpressionExecutionContext, LiquidPropertyAccessor>("Configuration", x => new LiquidPropertyAccessor(name => ToFluidValue(GetConfigurationValue(name), options)));
            memberAccessStrategy.Register<ConfigurationSectionWrapper, ConfigurationSectionWrapper?>((source, name) => source.GetSection(name));
        }

        // Register all variable types.
        foreach (var variableDescriptor in _managementOptions.VariableDescriptors.Where(x => x.Type is { IsClass: true, ContainsGenericParameters: false }))
            memberAccessStrategy.Register(variableDescriptor.Type);

        return Task.CompletedTask;
    }

    private ConfigurationSectionWrapper GetConfigurationValue(string name) => new(_configuration.GetSection(name));
    private Task<FluidValue> ToFluidValue(object? input, TemplateOptions options) => Task.FromResult(FluidValue.Create(input, options));

    private Task<FluidValue> GetVariable(ExpressionExecutionContext context, string key, TemplateOptions options)
    {
        var value = GetVariableInScope(context, key);
        return Task.FromResult(value == null ? NilValue.Instance : FluidValue.Create(value, options));
    }

    private static object? GetVariableInScope(ExpressionExecutionContext context, string variableName)
    {
        var q = from variable in EnumerateVariablesInScope(context)
            where variable.Name == variableName
            where variable.TryGet(context, out _)
            select variable.Get(context);

        return q.FirstOrDefault();
    }

    private static IEnumerable<Variable> EnumerateVariablesInScope(ExpressionExecutionContext context)
    {
        var currentScope = context;

        while (currentScope != null)
        {
            if (!currentScope.TryGetActivityExecutionContext(out var activityExecutionContext))
                break;

            var variables = activityExecutionContext.Variables;

            foreach (var variable in variables)
                yield return variable;

            currentScope = currentScope.ParentContext;
        }
    }
}