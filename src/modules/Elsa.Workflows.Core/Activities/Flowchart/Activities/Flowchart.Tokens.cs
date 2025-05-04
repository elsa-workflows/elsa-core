using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

public partial class Flowchart
{
    private const string TokenStoreKey = "Flowchart.Tokens";
    private const string WaitAnyGuardKey = "Flowchart.WaitAnyGuard";

    private async ValueTask OnChildCompletedTokenBasedLogicAsync(ActivityCompletedContext ctx)
    {
        var flowContext = ctx.TargetContext;
        var completedActivity = ctx.ChildContext.Activity;
        var flowGraph = flowContext.GetFlowGraph();

        // Retrieve or initialize the “WaitAny” guard set.
        if (!flowContext.Properties.TryGetValue(WaitAnyGuardKey, out var waitAnyGuardObj) || waitAnyGuardObj is not HashSet<string> waitAnyGuard)
        {
            waitAnyGuard = new();
            flowContext.Properties[WaitAnyGuardKey] = waitAnyGuard;
        }

        // When a WaitAny‐joined activity actually completes, clear its flag so it can fire again on a next loopback.
        if (completedActivity.GetJoinMode() == FlowJoinMode.WaitAny)
            waitAnyGuard.Remove(completedActivity.Id);

        // Emit tokens.
        var outcomes = (ctx.Result as Outcomes ?? Outcomes.Default).Names;
        var outboundConnections = flowGraph.GetOutboundConnections(completedActivity);
        var activeOutboundConnections = outboundConnections.Where(x => outcomes.Contains(x.Source.Port)).Distinct().ToList();
        var tokens = GetTokenList(flowContext);

        foreach (var connection in activeOutboundConnections)
            tokens.Add(Token.Create(connection.Source.Activity, connection.Target.Activity));

        // Consume tokens.
        var inboundTokens = tokens.Where(t => t.ToActivityId == completedActivity.Id).ToList();
        foreach (var t in inboundTokens) 
            t.Consume();

        // Schedule next activities.
        foreach (var connection in activeOutboundConnections)
        {
            var targetActivity = connection.Target.Activity;
            var joinKind = targetActivity.GetJoinMode();

            if (joinKind == FlowJoinMode.WaitAny)
            {
                // only the first token per iteration will pass this check…
                if (waitAnyGuard.Add(targetActivity.Id))
                {
                    await flowContext.CancelInboundAncestorsAsync(targetActivity);
                    await flowContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);
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
                    await flowContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);
            }
        }

        SaveTokenList(flowContext, tokens);

        // Complete flow if done.
        var hasPendingWork = flowContext.HasPendingWork();
        var allTokensConsumed = tokens.All(t => t.Consumed);
        var hasFaulted = flowContext.HasFaultedChildren();

        if (!hasPendingWork && allTokensConsumed && !hasFaulted)
            await flowContext.CompleteActivityAsync();
    }

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