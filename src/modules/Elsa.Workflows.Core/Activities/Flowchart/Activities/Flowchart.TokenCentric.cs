using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

public partial class Flowchart
{
    private const string TokenStoreKey = "FlowchartTokens";

    private record Token(string FromActivityId, string Outcome, string ToActivityId, bool Consumed);

    private async ValueTask OnChildCompletedTokenBasedLogicAsync(ActivityCompletedContext context)
    {
        var flowchartContext = context.TargetContext;
        var completedActivityContext = context.ChildContext;

        if (flowchartContext.Activity != this)
            throw new InvalidOperationException("Target context activity must be this flowchart");

        // If the completed activity's status is anything but "Completed", do not schedule its outbound activities.
        if (completedActivityContext.Status != ActivityStatus.Completed)
            return;

        // If the complete activity is a terminal node, complete the flowchart immediately.
        var completedActivity = completedActivityContext.Activity;
        if (completedActivity is ITerminalNode)
        {
            await flowchartContext.CompleteActivityAsync();
            return;
        }

        // Determine the outcomes from the completed activity.
        var result = context.Result;
        var outcomes = result as Outcomes ?? Outcomes.Default;
        var outcomeNames = outcomes.Names;

        // Get outbound connections.
        var flowGraph = GetFlowGraph(flowchartContext);
        var outboundConnections = flowGraph.GetOutboundConnections(completedActivity);

        // Load or initialize the token list.
        var tokens = GetTokenList(flowchartContext);

        // Emit one token per activated outbound connection
        foreach (var outcome in outcomeNames)
        {
            var activatedConnections = outboundConnections.Where(c => c.Source.Port == outcome);
            var newTokens = activatedConnections.Select(connection => new Token(
                FromActivityId: connection.Source.Activity.Id,
                Outcome: outcome,
                ToActivityId: connection.Target.Activity.Id,
                Consumed: false)).ToList();
            tokens.AddRange(newTokens);
        }

        // Store the updated token list.
        SaveTokenList(flowchartContext, tokens);
        
        // For each activity that has incoming tokens, try an implicit‐join.
        var targets = tokens
            .Where(t => !t.Consumed)
            .Select(t => t.ToActivityId)
            .Distinct()
            .ToList();

        foreach (var targetId in targets)
        {
            var inbound = Connections
                .Where(c => c.Target.Activity.Id == targetId)
                .ToList();

            // Check we have ≥1 unconsumed token for each inbound connection.
            var ready = inbound.All(c =>
                tokens.Any(t =>
                    !t.Consumed &&
                    t.FromActivityId == c.Source.Activity.Id &&
                    t.Outcome        == c.Source.Port &&
                    t.ToActivityId   == targetId));

            if (!ready) 
                continue;

            // Consume exactly one token per inbound connection.
            foreach (var connection in inbound)
            {
                var token = tokens.First(t =>
                    !t.Consumed &&
                    t.FromActivityId == connection.Source.Activity.Id &&
                    t.Outcome        == connection.Source.Port &&
                    t.ToActivityId   == targetId);

                tokens.Remove(token);
                tokens.Add(token with { Consumed = true });
            }
            
            // Schedule the joined activity exactly once.
            var targetActivity = Activities.First(x => x.Id == targetId);
            await flowchartContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);
        }
        
        // Save token list.
        SaveTokenList(flowchartContext, tokens);

        // If nothing is scheduled and no tokens remain, complete the flowchart
        if (!flowchartContext.WorkflowExecutionContext.Scheduler.HasAny && tokens.All(t => t.Consumed))
            await context.CompleteActivityAsync();
    }

    // Helpers to store/retrieve tokens in the execution context
    private List<Token> GetTokenList(ActivityExecutionContext context)
    {
        if (context.Properties.TryGetValue(TokenStoreKey, out var obj) && obj is List<Token> list)
            return list;

        var newList = new List<Token>();
        context.Properties[TokenStoreKey] = newList;
        return newList;
    }

    private void SaveTokenList(ActivityExecutionContext context, List<Token> tokens)
    {
        context.Properties[TokenStoreKey] = tokens;
    }
}