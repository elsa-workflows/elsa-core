using System.Net.Mime;
using Elsa.Expressions.Models;
using Elsa.Http;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Samples.AspNet.WorkflowServer.Workflows;

/// <summary>
/// Represents a mysterious pond workflow.
/// </summary>
public class MysteriousPondWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var investment = builder.WithVariable<Investment>();

        builder.Root = new Sequence
        {
            Activities =
            {
                new HttpEndpoint
                {
                    Path = new("mysterious_pond"),
                    SupportedMethods = new([HttpMethod.Post.Method]),
                    ParsedContent = new(investment),
                    CanStartWorkflow = true
                },
                new WriteLine(context => $"Received {GetRupees(context)} rupees"),
                new Switch
                {
                    Cases =
                    {
                        new SwitchCase("Great Luck", context => GetRupees(context) >= 1000, WriteHttpResponse("For today, you will have great luck.")),
                        new SwitchCase("Good Luck", context => GetRupees(context) >= 500, WriteHttpResponse("For today, you will have good luck.")),
                        new SwitchCase("A Little Luck", context => GetRupees(context) >= 250, WriteHttpResponse("For today, you will have a little luck.")),
                        new SwitchCase("Bad Luck", context => GetRupees(context) < 250, WriteHttpResponse("For today, you will have bad luck.")),
                        
                    }
                }
            }
        };
        return;

        int GetRupees(ExpressionExecutionContext context) => investment.Get(context)!.Rupees;
    }
    
    private static WriteHttpResponse WriteHttpResponse(string message) => new()
    {
        Content = new(message),
        ContentType = new(MediaTypeNames.Text.Plain),
    };

    private record Investment(int Rupees);
};