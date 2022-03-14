using Elsa.Activities.OpcUa.Configuration;
using Elsa.Activities.OpcUa.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Opc.Ua.Client;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa
{
    [Trigger(
        Category = "OpcUa",
        DisplayName = "OpcUa Message Received",
        Description = "Triggers when OpcUa item changed",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class OpcUaMessageReceived : Activity
    {
        [ActivityInput(
            Hint = "OpcUa connection string [opc.tcp://localhost:50000]",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            Order = 1,
            Category = PropertyCategories.Configuration)]
        public string ConnectionString { get; set; } = default!;


        [ActivityInput(
            Hint = "OperationTimeout",
            SupportedSyntaxes = new[] { SyntaxNames.Json },
            Order = 2,
            Category = PropertyCategories.Configuration)]
        public int OperationTimeout { get; set; } = 15000;

        [ActivityInput(
            Hint = "SessionTimeout",
            SupportedSyntaxes = new[] { SyntaxNames.Json },
            Order = 2,
            Category = PropertyCategories.Configuration)]
        public int SessionTimeout { get; set; } = 60000;

        [ActivityInput(
            Hint = "PublishingInterval",
            SupportedSyntaxes = new[] { SyntaxNames.Json },
            Order = 2,
            Category = PropertyCategories.Configuration)]
        public int PublishingInterval { get; set; } = 1000;

        [ActivityInput(
            Hint = "List of item monitored",
            Order = 3,
            UIHint = ActivityInputUIHints.Dictionary,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json })]
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();


        public string ClientId => $"Elsa-{Id.ToUpper()}";

        [ActivityOutput(Hint = "Received message")]
        public object? Output { get; set; }
        
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternalAsync(context) : Suspend();

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternalAsync(context);

        private IActivityExecutionResult ExecuteInternalAsync(ActivityExecutionContext context)
        {
            var item = (MonitoredItem)context.Input!;
            //foreach (var value in item.DequeueValues())
            //{
            //    System.Console.WriteLine("{0}: {1}, {2}, {3}", item.DisplayName, value.Value, value.SourceTimestamp, value.StatusCode);
            //}

            Output = item;           
            context.LogOutputProperty(this, nameof(Output), Output);
            
            return Done();
        }

    }
}