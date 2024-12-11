using Elsa.Kafka.Activities;
using Elsa.Workflows;
using Elsa.Expressions.Models;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.JavaScript.Models;
using Elsa.Workflows.Activities;
using System.Text.Json;
using Elsa.Workflows.Activities.Flowchart.Models;
using Endpoint = Elsa.Workflows.Activities.Flowchart.Models.Endpoint;
using Elsa.Scheduling.Activities;
using Trimble.Elsa.Activities.Kafka;
using Elsa.Workflows.Contracts;

namespace Trimble.Elsa.Activities;

public class TestWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Name = "Producer Workflow Again";
        builder.WithVariable("message-var#name", "message-val");
        var scrapedCorrelationVariable = builder.WithVariable("scraped-correlation", "");
        var message = builder.WithVariable<AvroDataPropertyMessage>("received-message-1", null);

        var start = new Start();

        var produce = new ProduceMessage
        {
            Topic = new("elsa-test"),
            ProducerDefinitionId = new("trimble-avro-producer"),
            Content = new(new Expression("CSharp", @"

new Trimble.Elsa.Activities.Kafka.AvroDataPropertyMessage()
{
CorrelationId = CorrelationId, // loaded from Elsa.CSharp.Models.Globals
Data = new Dictionary<string, string>()
    {
        { ""CapabilityName"", Variables.Get<string>(""message-var#name"") },
        { ""time"", DateTimeOffset.Now.ToString() }
    }
}

")),
            Key = new("key")
        };
        var receive = new MessageReceived
        {
            ConsumerDefinitionId = new("trimble-avro-consumer"),
            Topics = new(["elsa-test"]),
            Predicate = new(JavaScriptExpression.Create("getVariable(\"message\").CorrelationId === getCorrelationId() && CorrelationId getVariable(\"message\").Data.CapabilityName === getVariable(\"message-var#name\")")),
            Result = new(message),
            CanStartWorkflow = true
        };
        var delay = new Delay(TimeSpan.FromSeconds(5));
        var write = new WriteLine(c => JsonSerializer.Serialize(message.Get(c)));

        var scrapeVariable1 = new SetVariable
        {
            Variable = scrapedCorrelationVariable,
            Value = new(JavaScriptExpression.Create("getVariable(\"received-message-1\").CorrelationId"))
        };

        var flowChart = new Flowchart
        {
            Activities =
            {
                start,
                delay,
                produce,
                receive,
                write,
               // scrapeVariable1
            },
            Connections = new List<Connection>()
            {
                new(new Endpoint(start, "Done"), new Endpoint(receive, "In")),
                new(new Endpoint(receive, "Done"), new Endpoint(write, "In")),
                new(new Endpoint(start, "Done"), new Endpoint(delay, "In")),
                new(new Endpoint(delay, "Done"), new Endpoint(produce, "In")),
               // new(new Endpoint(write, "Done"), new Endpoint(scrapeVariable1, "In"))
            },
            CanStartWorkflow = true,
            Start = start
        };

        builder.Root = flowChart;
    }
}