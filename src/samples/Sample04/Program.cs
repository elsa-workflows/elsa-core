using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Sample04.Activities;

namespace Sample04
{
    /// <summary>
    /// A strongly-typed, long-running workflows program demonstrating scripting, branching and resuming suspended workflows by providing user driven stimuli.
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .AddActivity<Sum>()
                .AddActivity<Subtract>()
                .AddActivity<Multiply>()
                .AddActivity<Divide>()
                .AddWorkflow<CalculatorWorkflow>()
                .BuildServiceProvider();

            // Create a workflow.
            var workflowFactory = services.GetRequiredService<IWorkflowFactory>();
            var workflow = workflowFactory.CreateWorkflow<CalculatorWorkflow>();

            // Start the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            var executionContext = await invoker.StartAsync(workflow);

            // Keep resuming the workflow until it completes.
            while (executionContext.Workflow.Status != WorkflowStatus.Finished)
            {
                // Print current execution log + blocking activities to visualize current workflow state.
                DisplayWorkflowState(executionContext.Workflow);
                
                var textInput = Console.ReadLine();
                var input = new Variables(new Dictionary<string, object>() { ["ReadLineInput"] = textInput });

                executionContext.Workflow.Input = input;
                executionContext = await invoker.ResumeAsync(executionContext.Workflow, executionContext.Workflow.BlockingActivities);
            }

            Console.WriteLine("Workflow has ended. Here are the activities that have executed:");
            DisplayWorkflowState(executionContext.Workflow);

            Console.ReadLine();
        }

        private static void DisplayWorkflowState(Workflow workflow)
        {
            var table = GetExecutionLogDataTable(workflow);
            table.Print();
        }

        private static DataTable GetExecutionLogDataTable(Workflow workflow)
        {
            var workflowDefinitionVersion = workflow.Definition;
            var table = new DataTable { TableName = workflowDefinitionVersion.Name };

            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("Type", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Faulted", typeof(bool));
            table.Columns.Add("Blocking", typeof(bool));
            table.Columns.Add("Message", typeof(string));
            table.Columns.Add("Timestamp", typeof(DateTime));

            foreach (var entry in workflow.ExecutionLog)
            {
                var activity = workflow.GetActivity(entry.ActivityId);
                var activityDefinition = workflowDefinitionVersion.GetActivity(activity.Id);

                table.Rows.Add(activity.Id, activity.Type, activityDefinition.DisplayName, activityDefinition.Description, entry.Faulted, false, entry.Message, entry.Timestamp.ToDateTimeUtc());
            }

            foreach (var activity in workflow.BlockingActivities)
            {
                var activityDefinition = workflowDefinitionVersion.GetActivity(activity.Id);
                table.Rows.Add(activity.Id, activity.Type, activityDefinition.DisplayName, activityDefinition.Description, false, true, "Waiting for input...", null);
            }

            return table;
        }
    }
}