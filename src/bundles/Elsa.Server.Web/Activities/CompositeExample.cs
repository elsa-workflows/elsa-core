using Elsa.AzureServiceBus.Activities;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Server.Web.Activities;

[Activity("Elsa", "Example")]
public class CompositeExample : Composite
{
    /// <summary>
    /// The name of the queue or topic to read from.
    /// </summary>
    [Input(Description = "The name of the queue or topic to read from.")]
    public Input<string> QueueOrTopic { get; set; } = default!;

    private MessageReceived _messageReceived = default!;

    /// <inheritdoc />
    public override void Setup()
    {
        _messageReceived = new MessageReceived
        {
            QueueOrTopic = QueueOrTopic,
            CanStartWorkflow = true
        };

        var writeLine = new WriteLine("Hello World!");

        Root = new Sequence
        {
            Activities =
            {
                _messageReceived,
                writeLine
            }
        };
    }
}