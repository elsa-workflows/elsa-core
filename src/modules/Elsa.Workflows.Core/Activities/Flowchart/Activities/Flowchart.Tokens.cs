using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Signals;

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
        var inboundTokens = tokens.Where(t => t.ToActivityId == completedActivity.Id && t is { Consumed: false, Blocked: false }).ToList();
        foreach (var t in inboundTokens)
            t.Consume();

        // Schedule next activities.
        foreach (var connection in activeOutboundConnections)
        {
            var targetActivity = connection.Target.Activity;
            var mergeMode = await targetActivity.GetMergeModeAsync(ctx.ChildContext);

            if (mergeMode is MergeMode.Stream or MergeMode.Race)
            {
                if (mergeMode == MergeMode.Race)
                    await flowContext.CancelInboundAncestorsAsync(targetActivity);

                // Check if there is any blocking token preventing the activity from being scheduled.
                var existingBlockedToken = tokens.FirstOrDefault(t => t.ToActivityId == targetActivity.Id && t.FromActivityId == connection.Source.Activity.Id && t.Outcome == connection.Source.Port && t.Blocked);

                if (existingBlockedToken == null)
                {
                    // Schedule the target activity.
                    await flowContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);

                    // And block other inbound connections.
                    var otherInboundConnections = flowGraph.GetForwardInboundConnections(targetActivity).Where(x => x.Source.Activity != completedActivity).ToList();

                    foreach (var inboundConnection in otherInboundConnections)
                    {
                        var blockedToken = Token.Create(inboundConnection.Source.Activity, inboundConnection.Target.Activity, inboundConnection.Source.Port).Block();
                        tokens.Add(blockedToken);
                    }
                }
                else
                {
                    // Consume the block.
                    existingBlockedToken.Consume();
                }
            }
            else
            {
                // Wait for all inbound tokens to be consumed before scheduling the target activity.
                var inboundConnections = flowGraph.GetForwardInboundConnections(targetActivity);
                var hasUnconsumed = inboundConnections.Any(inbound =>
                    tokens.Any(t => t is { Consumed: false, Blocked: false } && t.ToActivityId == inbound.Source.Activity.Id)
                );

                if (!hasUnconsumed)
                {
                    await flowContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);
                }
            }
        }

        // Complete flow if done.
        var hasPendingWork = flowContext.HasPendingWork();

        if (!hasPendingWork)
        {
            tokens.Clear();
            await flowContext.CompleteActivityAsync();
        }

        // Purge tokens.
        tokens.RemoveWhere(t => t.ToActivityId == completedActivity.Id && t.Consumed);
    }

    private async ValueTask OnTokenFlowActivityCanceledAsync(CancelSignal signal, SignalContext context)
    {
        var flowchartContext = context.ReceiverActivityExecutionContext;
        var cancelledActivityContext = context.SenderActivityExecutionContext;

        // Remove all tokens from and to this activity.
        var tokenList = GetTokenList(flowchartContext);
        tokenList.RemoveWhere(x => x.FromActivityId == cancelledActivityContext.Activity.Id || x.ToActivityId == cancelledActivityContext.Activity.Id);
        await CompleteIfNoPendingWorkAsync(flowchartContext);
    }

    internal List<Token> GetTokenList(ActivityExecutionContext context)
    {
        if (context.Properties.TryGetValue(TokenStoreKey, out var obj) && obj is List<Token> list)
            return list;

        var newList = new List<Token>();
        context.Properties[TokenStoreKey] = newList;
        return newList;
    }
}