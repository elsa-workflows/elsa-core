using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Reflection.Activities
{
    /// <summary>
    /// Execute a method by reflection.
    /// </summary>
    [ActivityDefinition(
        Category = "Reflection",
        Description = "Execute a method by reflection.",
        RuntimeDescription = "a => !!a.state.variableName ? `Execute a Method by reflection and store the result into <strong>${ a.state.variableName }</strong>.` : 'Execute a Method by reflection.'",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ExecuteMethod : Activity
    {
        [ActivityProperty(Hint = "An expression that returns an array of arguments to the method. Leave empty if the method does not accept any arguments.")]
        public WorkflowExpression<object[]> Arguments
        {
            get => GetState<WorkflowExpression<object[]>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The assembly-qualified type name containing the method to execute.")]
        public string TypeName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The name of the method name to execute.")]
        public string MethodName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var type = System.Type.GetType(TypeName);

            if (type == null)
                return Fault($"Type {TypeName} not found.");

            var inputValues = await context.EvaluateAsync(Arguments, cancellationToken) ?? Array.Empty<object>();

            var method = type
                .GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(x => x.GetParameters().Length == inputValues.Length)
                .FirstOrDefault(x => x.Name == MethodName);

            if (method == null)
                return Fault($"Type {TypeName} does not have a method called {MethodName}.");


            var instance = method.IsStatic ? default : ActivatorUtilities.GetServiceOrCreateInstance(context.ServiceProvider, type);
            var result = method.Invoke(instance, inputValues);

            Output.SetVariable("Result", result);
            return Outcome(OutcomeNames.Done);
        }
    }
}