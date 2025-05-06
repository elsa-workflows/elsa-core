using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

public partial class Flowchart
{
    private const string TokenStoreKey = "Flowchart.Tokens";

    private async ValueTask OnChildCompletedTokenBasedLogicAsync(ActivityCompletedContext ctx)
    {
        var flowContext = ctx.TargetContext;
        var completedActivity = ctx.ChildContext.Activity;
        var flowGraph = flowContext.GetFlowGraph();

        // Emit tokens.
        var outcomes = (ctx.Result as Outcomes ?? Outcomes.Default).Names;
        var outboundConnections = flowGraph.GetOutboundConnections(completedActivity);
        var activeOutboundConnections = outboundConnections.Where(x => outcomes.Contains(x.Source.Port)).Distinct().ToList();
        var tokens = GetTokenList(flowContext);

        foreach (var connection in activeOutboundConnections)
            tokens.Add(Token.Create(connection.Source.Activity, connection.Target.Activity, connection.Source.Port));

        // Consume tokens.
        var inboundTokens = tokens.Where(t => t.ToActivityId == completedActivity.Id).ToList();
        foreach (var t in inboundTokens)
            t.Consume();

        // Schedule next activities.
        foreach (var connection in activeOutboundConnections)
        {
            var targetActivity = connection.Target.Activity;
            var mergeMode = await targetActivity.GetMergeModeAsync(ctx.ChildContext);
            var attachedToken = tokens.First(t => t.FromActivityId == connection.Source.Activity.Id && t.ToActivityId == targetActivity.Id && t.Outcome == connection.Source.Port);

            if (mergeMode is MergeMode.Stream or MergeMode.Race)
            {
                // If there's at least one schedule token by the target, we don't schedule the target again.
                var hasScheduled = tokens.Any(t => (t.Scheduled || t.Consumed) && t.ToActivityId == targetActivity.Id);

                // Only the first token per iteration will pass this check.
                if (!hasScheduled)
                {
                    if(mergeMode == MergeMode.Race)
                        await flowContext.CancelInboundAncestorsAsync(targetActivity);
                    
                    attachedToken.Schedule();
                    await flowContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);
                }
                else
                {
                    // The target will not be scheduled again, so the outbound token would not be consumed upon its completion.
                    // So, we need to consume it manually.
                    attachedToken.Consume(); 
                }
            }
            else
            {
                // Wait for all inbound tokens to be consumed before scheduling the target activity.
                var inboundConnections = flowGraph.GetForwardInboundConnections(targetActivity);
                var hasUnconsumed = inboundConnections.Any(inbound =>
                    tokens.Any(t => !t.Consumed && t.ToActivityId == inbound.Source.Activity.Id)
                );

                if (!hasUnconsumed)
                {
                    attachedToken.Schedule();
                    await flowContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);
                }
            }
        }

        SaveTokenList(flowContext, tokens);

        // Complete flow if done.
        var hasPendingWork = flowContext.HasPendingWork();

        if (!hasPendingWork)
            await flowContext.CompleteActivityAsync();
    }

    internal List<Token> GetTokenList(ActivityExecutionContext context)
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