using Elsa.Workflows.Activities.Flowchart.Contracts;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

public partial class Flowchart
{
    private record Token(string FromActivityId, string? FromActivityName, string ToActivityId, string? ToActivityName, bool Consumed)
    {
        public static Token Create(IActivity from, IActivity to) => new(from.Id, from.Name, to.Id, to.Name, false);

        public Token Consume() => this with
        {
            Consumed = true
        };
    }

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
        var flowContext = ctx.TargetContext;
        var completedActivityContext = ctx.ChildContext;
        var completedActivity = completedActivityContext.Activity;

        // Check for abnormal state.
        if (flowContext.Activity != this) throw new InvalidOperationException();
        if (completedActivityContext.Status != ActivityStatus.Completed) return;

        // If this is a terminal node, there's nothing else to schedule.
        if (completedActivity is ITerminalNode)
        {
            await flowContext.CompleteActivityAsync();
            return;
        }

        var flowGraph = GetFlowGraph(flowContext);

        // Get the outcomes and matching outbound connections (i.e. active connections).
        var outcomes = (ctx.Result as Outcomes ?? Outcomes.Default).Names;
        var outboundConnections = GetFlowGraph(flowContext).GetOutboundConnections(completedActivity);
        var activeOutboundConnections = outboundConnections.Where(x => outcomes.Contains(x.Source.Port)).Distinct().ToList();

        // Emit a token for each active outbound connection.
        var tokens = GetTokenList(flowContext);

        foreach (var connection in activeOutboundConnections)
        {
            var sourceEndpoint = connection.Source;
            var sourceActivity = sourceEndpoint.Activity;
            var targetActivity = connection.Target.Activity;
            var newToken = Token.Create(sourceActivity, targetActivity);
            tokens.Add(newToken);
        }

        // Consume any inbound tokens.
        var inboundTokens = tokens.Where(t => t.ToActivityId == completedActivity.Id).ToList();

        foreach (var token in inboundTokens)
        {
            tokens.Remove(token);
            var consumedToken = token.Consume();
            tokens.Add(consumedToken);
        }

        // Consider each active outbound connection.
        foreach (var connection in activeOutboundConnections)
        {
            // For each target, get its inbound connections.
            var targetInboundConnections = flowGraph.GetForwardInboundConnections(connection.Target.Activity).ToList();
            
            // For each inbound connection, check to see if there are any unconsumed tokens.
            var unconsumedTokens = targetInboundConnections.Where(x => tokens.Where(token => !token.Consumed).Select(token => token.ToActivityId).Contains(x.Source.Activity.Id)).ToList();
            var hasUnconsumedTokens = unconsumedTokens.Count > 0;
            var ready = !hasUnconsumedTokens;
            
            if (ready) 
                await flowContext.ScheduleActivityAsync(connection.Target.Activity, OnChildCompletedTokenBasedLogicAsync);
        }

        // Save the updated token list.
        SaveTokenList(flowContext, tokens);

        // Complete if nothing left
        if (!flowContext.WorkflowExecutionContext.Scheduler.HasAny
            && tokens.All(t => t.Consumed))
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

    private bool IsLoopback(Connection connection) => _loopbackDetector.IsLoopback(connection);
}