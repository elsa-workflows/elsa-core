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

        // Emit tokens for active outcomes.
        var outcomes = (ctx.Result as Outcomes ?? Outcomes.Default).Names;
        var outboundConnections = flowGraph.GetOutboundConnections(completedActivity);
        var activeOutboundConnections = outboundConnections.Where(x => outcomes.Contains(x.Source.Port ?? "Done")).Distinct().ToList(); // Assume default port is "Done" if null.
        var tokens = GetTokenList(flowContext);

        foreach (var connection in activeOutboundConnections)
            tokens.Add(Token.Create(connection.Source.Activity, connection.Target.Activity, connection.Source.Port));

        // Consume inbound tokens to the completed activity.
        var inboundTokens = tokens.Where(t => t.ToActivityId == completedActivity.Id && !t.Consumed && !t.Blocked).ToList();
        foreach (var t in inboundTokens)
            t.Consume();

        // Schedule next activities based on merge modes.
        foreach (var connection in activeOutboundConnections)
        {
            var targetActivity = connection.Target.Activity;
            var mergeMode = await targetActivity.GetMergeModeAsync(ctx.ChildContext);

            switch (mergeMode)
            {
                case MergeMode.Stream:
                case MergeMode.Race:
                    if (mergeMode == MergeMode.Race)
                        await flowContext.CancelInboundAncestorsAsync(targetActivity);

                    // Check for existing blocked token on this specific connection.
                    var existingBlockedToken = tokens.FirstOrDefault(t =>
                        t.ToActivityId == targetActivity.Id &&
                        t.FromActivityId == connection.Source.Activity.Id &&
                        t.Outcome == connection.Source.Port &&
                        t.Blocked);

                    if (existingBlockedToken == null)
                    {
                        // Schedule the target.
                        await flowContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);

                        // Block other inbound connections (adjust per mode if needed).
                        var otherInboundConnections = flowGraph.GetForwardInboundConnections(targetActivity)
                            .Where(x => x.Source.Activity != completedActivity)
                            .ToList();

                        foreach (var inboundConnection in otherInboundConnections)
                        {
                            var blockedToken = Token.Create(inboundConnection.Source.Activity, inboundConnection.Target.Activity, inboundConnection.Source.Port).Block();
                            tokens.Add(blockedToken);
                        }
                    }
                    else
                    {
                        // Consume the block without scheduling.
                        existingBlockedToken.Consume();
                    }
                    break;

                case MergeMode.Converge:
                    // Strict WaitAll for multiple forwards; schedule on arrival for <=1 (e.g., loops).
                    var inboundConnectionsConverge = flowGraph.GetForwardInboundConnections(targetActivity);

                    if (inboundConnectionsConverge.Count > 1)
                    {
                        var hasAllTokens = inboundConnectionsConverge.All(inbound =>
                            tokens.Any(t =>
                                !t.Consumed &&
                                !t.Blocked &&
                                t.FromActivityId == inbound.Source.Activity.Id &&
                                t.ToActivityId == targetActivity.Id &&
                                t.Outcome == inbound.Source.Port
                            )
                        );

                        if (hasAllTokens)
                            await flowContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);
                    }
                    else
                    {
                        await flowContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);
                    }
                    break;

                case MergeMode.None:
                default:
                    // Approximation that proceeds on dead paths.
                    var inboundConnectionsNone = flowGraph.GetForwardInboundConnections(targetActivity);
                    var hasUnconsumed = inboundConnectionsNone.Any(inbound =>
                        tokens.Any(t => !t.Consumed && !t.Blocked && t.ToActivityId == inbound.Source.Activity.Id)
                    );

                    if (!hasUnconsumed)
                        await flowContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);
                    break;
            }
        }

        // Complete flowchart if no pending work.
        if (!flowContext.HasPendingWork())
        {
            tokens.Clear();
            await flowContext.CompleteActivityAsync();
        }

        // Purge consumed tokens for the completed activity.
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