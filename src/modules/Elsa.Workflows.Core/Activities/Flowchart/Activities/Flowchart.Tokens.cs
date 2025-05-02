using Elsa.Workflows.Activities.Flowchart.Contracts;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

public partial class Flowchart
{
    private record Token(string FromActivityId, string FromActivityName, string Outcome, string ToActivityId, string ToActivityName, JoinKind JoinKind, bool Consumed);

    private const string TokenStoreKey = "FlowchartTokens";
    private const string DynExpectedCountKey = "FlowchartDynExpectedCount";
    private LoopbackDetector _loopbackDetector;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        _loopbackDetector = new(Activities, Connections);
        return base.ExecuteAsync(context);
    }

    private async ValueTask OnChildCompletedTokenBasedLogicAsync(ActivityCompletedContext ctx)
    {
        var flow = ctx.TargetContext;
        var childCtx = ctx.ChildContext;
        var act = childCtx.Activity;

        if (flow.Activity != this) throw new InvalidOperationException();
        if (childCtx.Status != ActivityStatus.Completed) return;
        if (act is ITerminalNode)
        {
            await flow.CompleteActivityAsync();
            return;
        }

        // 1) Emit tokens (no JoinKind on the token)
        var outcomes = (ctx.Result as Outcomes ?? Outcomes.Default).Names;
        var outbound = GetFlowGraph(flow).GetOutboundConnections(act);
        var tokens = GetTokenList(flow);

        foreach (var outcome in outcomes)
        foreach (var conn in outbound.Where(x => x.Source.Port == outcome))
            tokens.Add(new Token(
                FromActivityId: conn.Source.Activity.Id,
                FromActivityName: conn.Source.Activity.Name!,
                Outcome: outcome,
                ToActivityId: conn.Target.Activity.Id,
                ToActivityName: conn.Target.Activity.Name!,
                JoinKind.StaticAnd,
                Consumed: false));

        SaveTokenList(flow, tokens);

        // 2) Find every downstream target with pending tokens
        var targets = tokens.Where(t => !t.Consumed)
            .Select(t => t.ToActivityId)
            .Distinct()
            .ToList();

        foreach (var tid in targets)
        {
            var target = Activities.First(a => a.Id == tid);
            var inbound = Connections.Where(c => c.Target.Activity.Id == tid).ToList();
            var pending = tokens.Where(t => !t.Consumed && t.ToActivityId == tid).ToList();

            // 2a) If any inbound edge is a loop-back, treat this target as OR:
            var loopbacks = inbound.Where(IsLoopback).ToList();
            if (loopbacks.Any())
            {
                // fire once per loop-back token
                var loopTokens = pending
                    .Where(t => loopbacks.Any(c =>
                        c.Source.Activity.Id == t.FromActivityId &&
                        c.Source.Port == t.Outcome))
                    .ToList();

                foreach (var lt in loopTokens)
                {
                    tokens.Remove(lt);
                    var consumedToken = lt with
                    {
                        Consumed = true
                    };
                    tokens.Add(consumedToken);
                    SaveTokenList(flow, tokens);
                    await flow.ScheduleActivityAsync(target, OnChildCompletedTokenBasedLogicAsync);
                }

                continue;
            }

            // 2b) Otherwise, check for an explicit override on the activity:
            var explicitKind = target.GetJoinKind();
            var joinMode = explicitKind
                           ?? (inbound.Any(c => c.Source.Activity is IJoinHintProvider)
                               ? JoinKind.DynamicAnd
                               : JoinKind.StaticAnd);

            switch (joinMode)
            {
                case JoinKind.DynamicAnd:
                    // merge only the branches that actually produced tokens:
                    var activeConns = inbound
                        .Where(c => pending.Any(t =>
                            t.FromActivityId == c.Source.Activity.Id &&
                            t.Outcome == c.Source.Port))
                        .ToList();

                    // wait until each active branch has at least one token
                    if (activeConns.All(c => pending.Any(t =>
                            t.FromActivityId == c.Source.Activity.Id &&
                            t.Outcome == c.Source.Port)))
                    {
                        // schedule exactly once
                        await flow.ScheduleActivityAsync(target, OnChildCompletedTokenBasedLogicAsync);

                        // consume exactly one token per active path
                        foreach (var c in activeConns)
                        {
                            var tok = pending.First(t =>
                                t.FromActivityId == c.Source.Activity.Id &&
                                t.Outcome == c.Source.Port);
                            tokens.Remove(tok);
                            tok = tok with
                            {
                                Consumed = true
                            };
                            tokens.Add(tok);
                        }

                        SaveTokenList(flow, tokens);
                    }

                    break;

                case JoinKind.StaticAnd:
                    // wait for each *defined* inbound connection to have a token
                    if (inbound.All(c => pending.Any(t =>
                            t.FromActivityId == c.Source.Activity.Id &&
                            t.Outcome == c.Source.Port)))
                    {
                        await flow.ScheduleActivityAsync(target, OnChildCompletedTokenBasedLogicAsync);

                        foreach (var c in inbound)
                        {
                            var tok = pending.First(t =>
                                t.FromActivityId == c.Source.Activity.Id &&
                                t.Outcome == c.Source.Port);
                            tokens.Remove(tok);
                            tok = tok with
                            {
                                Consumed = true
                            };
                            tokens.Add(tok);
                        }

                        SaveTokenList(flow, tokens);
                    }

                    break;
            }
        }

        // 3) Complete if nothing left
        SaveTokenList(flow, tokens);
        if (!flow.WorkflowExecutionContext.Scheduler.HasAny
            && tokens.All(t => t.Consumed))
            await flow.CompleteActivityAsync();
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