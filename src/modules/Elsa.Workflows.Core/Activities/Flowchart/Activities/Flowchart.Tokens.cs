using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

public partial class Flowchart
{
    private record Token(string FromActivityId, string Outcome, string ToActivityId, bool Consumed);

    private const string TokenStoreKey = "FlowchartTokens";
    private LoopbackDetector _loopbackDetector;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        _loopbackDetector = new(Activities, Connections);
        return base.ExecuteAsync(context);
    }

    private async ValueTask OnChildCompletedTokenBasedLogicAsync(ActivityCompletedContext context)
    {
        var flowContext = context.TargetContext;
        var childContext = context.ChildContext;
        var completed = childContext.Activity;

        if (flowContext.Activity != this)
            throw new InvalidOperationException("Target context activity must be this flowchart");

        if (childContext.Status != ActivityStatus.Completed)
            return;

        if (completed is ITerminalNode)
        {
            await flowContext.CompleteActivityAsync();
            return;
        }

        // 1) Emit tokens.
        var outcomes = (context.Result as Outcomes ?? Outcomes.Default).Names;
        var outbound = GetFlowGraph(flowContext).GetOutboundConnections(completed);
        var tokens = GetTokenList(flowContext);
        tokens.AddRange(
            from outcome in outcomes
            from conn in outbound.Where(c => c.Source.Port == outcome)
            select new Token(FromActivityId: conn.Source.Activity.Id, Outcome: outcome, ToActivityId: conn.Target.Activity.Id, Consumed: false)
        );

        SaveTokenList(flowContext, tokens);

        // 2) Find all targets with unconsumed tokens.
        var targets = tokens
            .Where(t => !t.Consumed)
            .Select(t => t.ToActivityId)
            .Distinct()
            .ToList();

        // 3) For each target, run AND‐join on forward edges, OR‐join on loopbacks.
        foreach (var targetId in targets)
        {
            var inbound = Connections.Where(c => c.Target.Activity.Id == targetId).ToList();
            var loops = inbound.Where(IsLoopback).ToList();
            var forwards = inbound.Except(loops).ToList();

            // AND‐join for forward connections.
            if (forwards.Any())
            {
                var readyForwards = forwards.All(conn =>
                    tokens.Any(t =>
                        !t.Consumed &&
                        t.FromActivityId == conn.Source.Activity.Id &&
                        t.Outcome == conn.Source.Port &&
                        t.ToActivityId == targetId));

                if (readyForwards)
                {
                    // Consume one token per forward connection.
                    foreach (var connection in forwards)
                    {
                        var token = tokens.First(t =>
                            !t.Consumed &&
                            t.FromActivityId == connection.Source.Activity.Id &&
                            t.Outcome == connection.Source.Port &&
                            t.ToActivityId == targetId);

                        tokens.Remove(token);
                        tokens.Add(token with
                        {
                            Consumed = true
                        });
                    }

                    var targetActivity = Activities.First(a => a.Id == targetId);
                    await flowContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);
                }
            }

            // OR‐join for loopback connections.
            foreach (var conn in loops)
            {
                // All unconsumed tokens for this loopback.
                var loopTokens = tokens
                    .Where(t =>
                        !t.Consumed &&
                        t.FromActivityId == conn.Source.Activity.Id &&
                        t.Outcome == conn.Source.Port &&
                        t.ToActivityId == targetId)
                    .ToList();

                foreach (var token in loopTokens)
                {
                    tokens.Remove(token);
                    tokens.Add(token with
                    {
                        Consumed = true
                    });
                    
                    var targetActivity = Activities.First(a => a.Id == targetId);
                    await flowContext.ScheduleActivityAsync(targetActivity, OnChildCompletedTokenBasedLogicAsync);
                }
            }
        }
        
        SaveTokenList(flowContext, tokens);

        // 4) If nothing is pending and all tokens are consumed, complete.
        if (!flowContext.WorkflowExecutionContext.Scheduler.HasAny && GetTokenList(flowContext).All(t => t.Consumed))
        {
            await flowContext.CompleteActivityAsync();
        }
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

    private bool IsLoopback(Connection connection) => _loopbackDetector.IsLoopback(connection);
}