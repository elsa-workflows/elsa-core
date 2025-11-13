using Parlot;
using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using Elsa.Scripting.ElsaScript.Parsers.Containers;
using Elsa.Scripting.ElsaScript.Parsers.Expressions;

namespace Elsa.Scripting.ElsaScript.Parsers.ControlFlow;

public static class ForStatementParser
{
    public static readonly Parser<Statement> Instance = For();

    private static Parser<Statement> For()
    {
        var initializer = StatementParser.VarDeclaration()
            .Or(StatementParser.LetDeclaration())
            .Or(StatementParser.ConstDeclaration())
            .Or(ExpressionParser.Instance.Then<Statement>(e => new InlineStatement { Expression = e as InlineExpression ?? new InlineExpression { Code = e switch { IdentifierExpression id => id.Name, LiteralExpression lit => lit.Value?.ToString() ?? string.Empty, _ => string.Empty } } }));

        return new Parser<Statement>((ctx, out Statement value) =>
        {
            value = default!;
            if (!Parsers.Terms.Text("for").TryParse(ctx, out _)) return false;
            if (!Parsers.Terms.Char('(').TryParse(ctx, out _)) return false;
            if (!initializer.TryParse(ctx, out var init)) return false;
            if (!Parsers.Terms.Char(';').TryParse(ctx, out _)) return false;
            if (!ExpressionParser.Instance.TryParse(ctx, out var cond)) return false;
            if (!Parsers.Terms.Char(';').TryParse(ctx, out _)) return false;
            if (!ExpressionParser.Instance.TryParse(ctx, out var iterExpr)) return false;
            if (!Parsers.Terms.Char(')').TryParse(ctx, out _)) return false;
            if (!BlockParser.Instance.TryParse(ctx, out var body)) return false;

            value = new ForStatement
            {
                Initializer = init,
                Condition = cond,
                Iterator = new InlineStatement { Expression = iterExpr as InlineExpression ?? new InlineExpression { Code = string.Empty } },
                Body = body
            };
            return true;
        });
    }
}
