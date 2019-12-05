using System;
using System.IO;
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
    /// Load a dll by reflection.
    /// </summary>
    [ActivityDefinition(
        Category = "Reflection",
        Description = "Load a assembly by reflection.",
        RuntimeDescription = "a => !!a.state.assemblyName ? `Load a dll by reflection: <strong>${ a.state.assemblyName }</strong>.` : 'Load a dll by reflection.'",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class LoadAssembly : Activity
    {


        [ActivityProperty(Hint = "The assembly name to load")]
        public string AssemblyName
        {
            get => GetState<string>();
            set => SetState(value);
        }

        private string GetAssemblyPath(string AssemblyName)
        {
            return
                Directory.EnumerateFiles(
                    AppDomain.CurrentDomain.BaseDirectory,
                    AssemblyName,
                    SearchOption.AllDirectories)
                    .FirstOrDefault();
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            string assemblyPath = GetAssemblyPath(AssemblyName);

            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new FileNotFoundException(
                    "Could not find assembly: " + 
                    AssemblyName + 
                    " in LoadAssembly activity: " + 
                    context.CurrentActivity.Id + 
                    ", " + 
                    context.CurrentActivity.Name +
                    " - workflow: " + 
                    context.Workflow.Definition.DefinitionId + 
                    ", " +
                    context.Workflow.Definition.Name);
            }

            Assembly assembly = Assembly.LoadFile(assemblyPath);

            return Outcome(OutcomeNames.Done);

        }
    }
}