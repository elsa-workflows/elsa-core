using Parlot;
using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using Elsa.Scripting.ElsaScript.Parsers.Expressions;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers;

/// <summary>
/// Parser for activity statements.
/// </summary>
public static class ActivityStatementParser
{
    public static readonly Parser<Statement> Instance = ActivityStatement();

    private static Parser<Statement> ActivityStatement()
    {
        // ('listen')? ActivityCall ('as' Identifier)?
        return ZeroOrOne(Terms.Text("listen"))
            .And(ActivityCall())
            .And(ZeroOrOne(Terms.Text("as").SkipAnd(Terms.Identifier())))
            .AndSkip(ZeroOrOne(Terms.Char(';')))
            .Then<Statement>(result => new ActivityStatement
            {
                IsListen = result.Item1.Item1.HasValue,
                Call = result.Item1.Item2,
                Alias = result.Item2
            });
    }

    private static Parser<ActivityCall> ActivityCall()
    {
        // Identifier '(' ArgList? ')'
        return Terms.Identifier()
            .AndSkip(Terms.Char('('))
            .And(ZeroOrOne(ArgumentList()))
            .AndSkip(Terms.Char(')'))
            .Then(result => new ActivityCall
            {
                Name = result.Item1,
                Arguments = result.Item2 ?? new List<Argument>()
            });
    }

    private static Parser<List<Argument>> ArgumentList()
    {
        // Arg (',' Arg)*
        return Separated(Terms.Char(','), Argument())
            .Then(args => args.ToList());
    }

    private static Parser<Argument> Argument()
    {
        // NamedArg | Expr
        return NamedArgument().Or(PositionalArgument());
    }

    private static Parser<Argument> NamedArgument()
    {
        // Identifier ':' Expr
        return Terms.Identifier()
            .AndSkip(Terms.Char(':'))
            .And(ExpressionParser.Instance)
            .Then(result => new Argument
            {
                Name = result.Item1,
                Value = result.Item2
            });
    }

    private static Parser<Argument> PositionalArgument()
    {
        return ExpressionParser.Instance
            .Then(expr => new Argument
            {
                Name = null,
                Value = expr
            });
    }
}
