using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;
using Fluid;
using Fluid.Values;

namespace Elsa.Scripting.Liquid
{
    public class LiquidExpressionEvaluator : IExpressionEvaluator
    {
        private readonly ILiquidTemplateManager liquidTemplateManager;
        public const string SyntaxName = "Liquid";

        public LiquidExpressionEvaluator(ILiquidTemplateManager liquidTemplateManager)
        {
            this.liquidTemplateManager = liquidTemplateManager;
        }

        public string Syntax => SyntaxName;

        public async Task<object> EvaluateAsync(string expression, Type type, WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            var templateContext = CreateTemplateContext(workflowExecutionContext);
            //var expressionContext = new WorkflowExecutionExpressionContext(templateContext, workflowExecutionContext);

            var result = await liquidTemplateManager.RenderAsync(expression, templateContext);
            return string.IsNullOrWhiteSpace(result) ? default : Convert.ChangeType(result, type);
        }

        private TemplateContext CreateTemplateContext(WorkflowExecutionContext workflowContext)
        {
            var context = new TemplateContext();
            
            context.MemberAccessStrategy.Register<LiquidPropertyAccessor, FluidValue>((obj, name) => obj.GetValueAsync(name));
            context.MemberAccessStrategy.Register<WorkflowExecutionContext>();
            context.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("Input", obj => new LiquidPropertyAccessor(name => ToFluidValue(obj.Workflow.Input, name)));
            context.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("Output", obj => new LiquidPropertyAccessor(name => ToFluidValue(obj.Workflow.Output, name)));
            context.MemberAccessStrategy.Register<WorkflowExecutionContext, LiquidPropertyAccessor>("Variables", obj => new LiquidPropertyAccessor(name => ToFluidValue(obj.GetVariables(), name)));

            context.Model = workflowContext;
            return context;
        }

        private Task<FluidValue> ToFluidValue(IDictionary<string, object> dictionary, string key)
        {
            return Task.FromResult(!dictionary.ContainsKey(key) ? default : FluidValue.Create(dictionary[key]));
        }
    }
}