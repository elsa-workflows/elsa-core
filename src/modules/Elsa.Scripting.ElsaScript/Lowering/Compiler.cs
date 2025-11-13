using Elsa.Scripting.ElsaScript.Ast;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Memory;
using Elsa.Extensions;

namespace Elsa.Scripting.ElsaScript.Lowering;

public class Compiler
{
    private readonly IActivityRegistryLookupService _activityLookup;

    public Compiler(IActivityRegistryLookupService activityLookup)
    {
        _activityLookup = activityLookup;
    }

    public async Task<IActivity> CompileAsync(Program program, CancellationToken ct = default)
    {
        var seq = new Sequence();

        foreach (var stmt in program.Statements)
        {
            var activity = await LowerStatementAsync(stmt, ct);
            if (activity != null)
                seq.Activities.Add(activity);
        }

        return seq;
    }

    private async Task<IActivity?> LowerStatementAsync(Statement stmt, CancellationToken ct)
    {
        switch (stmt)
        {
            case ActivityStatement a:
                return await LowerActivityStatementAsync(a, ct);
            case IfStatement i:
                return await LowerIfAsync(i, ct);
            case ForEachStatement f:
                return await LowerForEachAsync(f, ct);
            default:
                return null;
        }
    }

    private async Task<IActivity> LowerIfAsync(IfStatement node, CancellationToken ct)
    {
        var ifAct = new If
        {
            Condition = new Input<bool>(MapExpression(node.Condition))
        };

        ifAct.Then = await LowerStatementOrBlockAsync(node.Then, ct);
        if (node.Else is not null)
            ifAct.Else = await LowerStatementOrBlockAsync(node.Else, ct);

        return ifAct;
    }

    private async Task<IActivity> LowerForEachAsync(ForEachStatement node, CancellationToken ct)
    {
        var forEach = new ForEach
        {
            Items = new Input<ICollection<object>>(MapExpression(node.Items))
        };

        // CurrentValue binding to a variable of given name
        var variable = new Variable(node.VariableName);
        forEach.CurrentValue = new Output<object>(((Elsa.Expressions.Models.MemoryBlockReference)variable));

        // Body.
        forEach.Body = await LowerBlockAsync(node.Body, ct);
        return forEach;
    }

    private async Task<IActivity> LowerStatementOrBlockAsync(StatementOrBlock sob, CancellationToken ct)
    {
        if (sob.Block is not null)
            return await LowerBlockAsync(sob.Block, ct);
        if (sob.Statement is null)
            return new Sequence();
        return (await LowerStatementAsync(sob.Statement, ct))!;
    }

    private async Task<IActivity> LowerActivityStatementAsync(ActivityStatement stmt, CancellationToken ct)
    {
        if (string.Equals(stmt.Call.Name, nameof(WriteLine), StringComparison.Ordinal))
        {
            var wl = new WriteLine(new Input<string>(MapSingleArgAsExpression(stmt)));
            if (stmt.IsListen) wl.SetCanStartWorkflow(true);
            if (!string.IsNullOrWhiteSpace(stmt.Alias)) wl.Id = stmt.Alias;
            return wl;
        }

        var descriptor = await _activityLookup.FindAsync(d => d.Name == stmt.Call.Name || d.TypeName.EndsWith($".{stmt.Call.Name}", StringComparison.Ordinal));
        if (descriptor == null)
            throw new InvalidOperationException($"Unknown activity '{stmt.Call.Name}'.");

        var activity = descriptor.Constructor(new ActivityConstructorContext(descriptor, default, new System.Text.Json.JsonSerializerOptions()));

        var inputMap = descriptor.GetWrappedInputProperties(activity);
        var positionalIndex = 0;
        foreach (var arg in stmt.Call.Arguments)
        {
            if (string.IsNullOrEmpty(arg.Name))
            {
                if (positionalIndex >= inputMap.Count) break;
                var target = inputMap.ElementAt(positionalIndex);
                var input = CreateInputFor(target.Value!.Type.GenericTypeArguments.FirstOrDefault() ?? typeof(object), arg.Value);
                descriptor.GetWrappedInputPropertyDescriptor(activity, target.Key)!.ValueSetter(activity, input);
                positionalIndex++;
            }
            else
            {
                var target = descriptor.GetWrappedInputProperty(activity, arg.Name);
                if (target == null) continue;
                var input = CreateInputFor(target.Type.GenericTypeArguments.FirstOrDefault() ?? typeof(object), arg.Value);
                descriptor.GetWrappedInputPropertyDescriptor(activity, arg.Name)!.ValueSetter(activity, input);
            }
        }

        if (stmt.IsListen) activity.SetCanStartWorkflow(true);
        if (!string.IsNullOrWhiteSpace(stmt.Alias)) activity.Id = stmt.Alias;
        return activity;
    }

    private Input CreateInputFor(Type nakedType, Ast.Expression value)
    {
        var wrappedType = typeof(Input<>).MakeGenericType(nakedType);
        var expr = MapExpression(value);
        return (Input)Activator.CreateInstance(wrappedType, expr)!;
    }

    private Elsa.Expressions.Models.Expression MapExpression(Ast.Expression expr)
    {
        switch (expr)
        {
            case LiteralExpression l:
                return Elsa.Expressions.Models.Expression.LiteralExpression(l.Value);
            case IdentifierExpression id:
                return new Elsa.Expressions.Models.Expression("Variable", new Variable(id.Name));
            case InlineExpression ie:
                var syntax = string.IsNullOrWhiteSpace(ie.Language) ? "JavaScript" : LanguageToSyntax(ie.Language);
                return new Elsa.Expressions.Models.Expression(syntax, ie.Code);
            case TemplateStringExpression ts:
                var js = BuildTemplateString(ts);
                return new Elsa.Expressions.Models.Expression("JavaScript", js);
            default:
                return Elsa.Expressions.Models.Expression.LiteralExpression(null);
        }
    }

    private static string BuildTemplateString(TemplateStringExpression ts)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append('`');
        foreach (var part in ts.Parts)
        {
            if (part.Text is not null)
                sb.Append(part.Text.Replace("`", "\\``"));
            else if (part.Expression is not null)
            {
                if (part.Expression is InlineExpression inl)
                    sb.Append("${" + inl.Code + "}");
                else if (part.Expression is IdentifierExpression id)
                    sb.Append("${" + id.Name + "}");
                else if (part.Expression is LiteralExpression lit)
                    sb.Append("${" + System.Text.Json.JsonSerializer.Serialize(lit.Value) + "}");
            }
        }
        sb.Append('`');
        return sb.ToString();
    }

    private static string LanguageToSyntax(string language)
    {
        return language switch
        {
            "js" => "JavaScript",
            "cs" => "CSharp",
            "py" => "Python",
            "liquid" => "Liquid",
            _ => language
        };
    }

    private Elsa.Expressions.Models.Expression MapSingleArgAsExpression(ActivityStatement stmt)
    {
        var arg = stmt.Call.Arguments.FirstOrDefault();
        if (arg == null) return Elsa.Expressions.Models.Expression.LiteralExpression(string.Empty);
        return MapExpression(arg.Value);
    }
}
