using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Reflection.Activities
{
    /// <summary>
    /// Execute a Method by reflection.
    /// </summary>
    [ActivityDefinition(
        Category = "Reflection",
        Description = "Execute a Method by reflection.",
        RuntimeDescription = "a => !!a.state.variableName ? `Execute a Method by reflection and store the result into <strong>${ a.state.variableName }</strong>.` : 'Execute a Method by reflection.'",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ExecuteMethod : Activity
    {

        public ExecuteMethod(IWorkflowExpressionEvaluator evaluator)
        {

        }


        [ActivityProperty(Hint = "The name of the variable to get the value from.")]
        public string InputVariableName
        {
            get => GetState<string>(null, "InputVariableName");
            set => SetState(value, "InputVariableName");
        }

        [ActivityProperty(Hint = "The name of the variable to store the value into.")]
        public string OutputVariableName
        {
            get => GetState<string>(null, "OutputVariableName");
            set => SetState(value, "OutputVariableName");
        }

        [ActivityProperty(Hint = "Assembly name (fullname or filename) to load")]
        public string AssemblyName
        {
            get => GetState<string>(null, "AssemblyName");
            set => SetState(value, "AssemblyName");
        }

        [ActivityProperty(Hint = "Class name to start or lookup")]
        public string ClassName
        {
            get => GetState<string>(null, "ClassName");
            set => SetState(value, "ClassName");
        }

        [ActivityProperty(Hint = "Class is a static class?")]
        public bool IsStaticClass
        {
            get => GetState<bool>(null, "IsStaticClass");
            set => SetState(value, "IsStaticClass");
        }

        [ActivityProperty(Hint = "Method name to execute")]
        public string MethodName
        {
            get => GetState<string>(null, "MethodName");
            set => SetState(value, "MethodName");
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            await Task.Delay(1);
            var input = context.GetVariable(InputVariableName);
            return Execute(context, input);
        }

        protected override ActivityExecutionResult OnResume(WorkflowExecutionContext context)
        {
            var input = context.GetVariable(InputVariableName);
            return Execute(context, input);
        }

        private ActivityExecutionResult Execute(WorkflowExecutionContext workflowContext, object receivedInput)
        {
            string path = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, AssemblyName, SearchOption.AllDirectories).FirstOrDefault();
            Assembly assembly = Assembly.LoadFrom(path);
            object receivedOutput = null;

            if (IsStaticClass)
            {
                var staticClassType = assembly.DefinedTypes.Where(t => t.Name == ClassName).FirstOrDefault();
                var staticMethod = staticClassType.DeclaredMethods.Where(m => m.Name == MethodName).FirstOrDefault();
                receivedOutput = staticMethod.Invoke(null, new object[1] { receivedInput });
            }
            else
            {

            }

            workflowContext.CurrentScope.SetVariable(OutputVariableName, receivedOutput);

            workflowContext.SetLastResult(receivedOutput);


            return Outcome(OutcomeNames.Done);
        }
    }
}