using Parlot;
using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using Elsa.Scripting.ElsaScript.Parsers.Containers;
using Elsa.Scripting.ElsaScript.Parsers.Expressions;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers.ControlFlow;

public static class ForStatementParser
{
    public static readonly Parser<Statement> Instance = For();

    private static Parser<Statement> For()
    {
        return new Parser<Statement>((ctx, out Statement value) =>
        {
            value = default!;
            if (!Terms.Text("for").TryParse(ctx, out _)) return false;
            if (!Terms.Char('(').TryParse(ctx, out _)) return false;

            // Parse initializer (var/let/const declaration or expression)
            Statement? initializer = null;
            if (StatementParser.VarDeclaration().TryParse(ctx, out var varDecl))
                initializer = varDecl;
            else if (StatementParser.LetDeclaration().TryParse(ctx, out var letDecl))
                initializer = letDecl;
            else if (StatementParser.ConstDeclaration().TryParse(ctx, out var constDecl))
                initializer = constDecl;
            else if (ExpressionParser.Instance.TryParse(ctx, out var expr))
                initializer = new InlineStatement { Expression = expr as InlineExpression ?? new InlineExpression { Code = expr.ToString() ?? "" } };

            if (!Terms.Char(';').TryParse(ctx, out _)) return false;

            // Parse condition
            if (!ExpressionParser.Instance.TryParse(ctx, out var condition)) return false;
            if (!Terms.Char(';').TryParse(ctx, out _)) return false;

            // Parse iterator
            if (!ExpressionParser.Instance.TryParse(ctx, out var iterator)) return false;
            if (!Terms.Char(')').TryParse(ctx, out _)) return false;

            // Parse body
            if (!BlockParser.Instance.TryParse(ctx, out var body)) return false;

            value = new ForStatement
            {
                Initializer = initializer,
                Condition = condition,
                Iterator = new InlineStatement { Expression = iterator as InlineExpression ?? new InlineExpression { Code = iterator.ToString() ?? "" } },
                Body = body
            };
            return true;
        });
    }
}
