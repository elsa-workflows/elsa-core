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
        var completedActivityContext = ctx.ChildContext;
        var flowGraph = flowContext.GetFlowGraph();
        var tokens = GetTokenList(flowContext);

        // If the completed activity is a terminal node, complete the flowchart immediately.
        if (completedActivity is ITerminalNode)
        {
            tokens.Clear();
            await flowContext.CompleteActivityAsync();
            return;
        }

        // Check if the completed activity is a direct child of this flowchart.
        // If not, skip flowchart-specific processing as the activity is managed by an intermediate container (e.g., sub-process).
        var isDirectChild = completedActivityContext.ParentActivityExecutionContext == flowContext;
        
        if (!isDirectChild)
        {
            // The activity is not a direct child, so we don't process its tokens.
            // Instead, just check if the flowchart should complete.
            if (!flowContext.HasPendingWork())
            {
                tokens.Clear();
                await flowContext.CompleteActivityAsync();
            }
            return;
        }

        // Emit tokens for active outcomes.
        var outcomes = (ctx.Result as Outcomes ?? Outcomes.Default).Names;
        var outboundConnections = flowGraph.GetOutboundConnections(completedActivity);
        var activeOutboundConnections = outboundConnections.Where(x => outcomes.Contains(x.Source.Port)).Distinct().ToList();

        foreach (var connection in activeOutboundConnections)
            tokens.Add(Token.Create(connection.Source.Activity, connection.Target.Activity, connection.Source.Port));

        // Consume inbound tokens to the completed activity.
        var inboundTokens = tokens.Where(t => t.ToActivityId == completedActivity.Id && t is { Consumed: false, Blocked: false }).ToList();
        foreach (var t in inboundTokens)
            t.Consume();

        // Schedule next activities based on merge modes.
        foreach (var connection in activeOutboundConnections)
        {
            var targetActivity = connection.Target.Activity;
            var mergeMode = await targetActivity.GetMergeModeAsync(ctx.ChildContext);

            switch (mergeMode)
            {
                case MergeMode.Cascade:
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

                case MergeMode.Merge:
                    // Wait for tokens from all forward inbound connections.
                    // Unlike Converge, this ignores backward connections (loops).
                    // Schedule on arrival for <=1 forward inbound (e.g., loops, sequential).
                    var inboundConnectionsMerge = flowGraph.GetForwardInboundConnections(targetActivity);

                    if (inboundConnectionsMerge.Count > 1)
                    {
                        var hasAllTokens = inboundConnectionsMerge.All(inbound =>
                            tokens.Any(t =>
                                t is { Consumed: false, Blocked: false } &&
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

                case MergeMode.Converge:
                    // Strictest mode: Wait for tokens from ALL inbound connections (forward + backward).
                    // Requires every possible inbound path to execute before proceeding.
                    var allInboundConnectionsConverge = flowGraph.GetInboundConnections(targetActivity);

                    if (allInboundConnectionsConverge.Count > 1)
                    {
                        var hasAllTokens = allInboundConnectionsConverge.All(inbound =>
                            tokens.Any(t =>
                                t is { Consumed: false, Blocked: false } &&
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

                case MergeMode.Stream:
                default:
                    // Flows freely - approximation that proceeds when upstream completes, ignoring dead paths.
                    var inboundConnectionsStream = flowGraph.GetForwardInboundConnections(targetActivity);
                    var hasUnconsumed = inboundConnectionsStream.Any(inbound =>
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