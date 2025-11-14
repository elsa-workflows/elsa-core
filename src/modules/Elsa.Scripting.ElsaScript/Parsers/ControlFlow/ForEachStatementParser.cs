using Parlot;
using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using Elsa.Scripting.ElsaScript.Parsers.Containers;
using Elsa.Scripting.ElsaScript.Parsers.Expressions;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers.ControlFlow;

public static class ForEachStatementParser
{
    public static readonly Parser<Statement> Instance = ForEach();

    private static Parser<Statement> ForEach()
    {
        return new Parser<Statement>((ctx, out Statement value) =>
        {
            value = default!;
            if (!Terms.Text("foreach").TryParse(ctx, out _)) return false;
            if (!Terms.Char('(').TryParse(ctx, out _)) return false;

            // Optional var/let
            string? varType = null;
            if (Terms.Text("var").TryParse(ctx, out _))
                varType = "var";
            else if (Terms.Text("let").TryParse(ctx, out _))
                varType = "let";

            // Variable name
            if (!Terms.Identifier().TryParse(ctx, out var varName)) return false;

            // "in"
            if (!Terms.Text("in").TryParse(ctx, out _)) return false;

            // Items expression
            if (!ExpressionParser.Instance.TryParse(ctx, out var items)) return false;

            if (!Terms.Char(')').TryParse(ctx, out _)) return false;

            // Body
            if (!BlockParser.Instance.TryParse(ctx, out var body)) return false;

            value = new ForEachStatement
            {
                VariableName = varName.ToString(),
                Items = items,
                Body = body
            };
            return true;
        });
    }
}
