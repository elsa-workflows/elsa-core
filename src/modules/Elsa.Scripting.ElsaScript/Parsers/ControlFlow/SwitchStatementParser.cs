using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using Elsa.Scripting.ElsaScript.Parsers.Containers;
using Elsa.Scripting.ElsaScript.Parsers.Expressions;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers.ControlFlow;

public static class SwitchStatementParser
{
    public static readonly Parser<Statement> Instance = Switch();

    private static Parser<Statement> Switch()
    {
        var caseParser = Terms.Text("case")
            .SkipAnd(ExpressionParser.Instance)
            .AndSkip(Terms.Char(':'))
            .And(BlockParser.Instance)
            .Then<SwitchCase>(t => new SwitchCase { Value = t.Item1, Body = t.Item2 });

        var defaultParser = Terms.Text("default")
            .SkipAnd(Terms.Char(':'))
            .SkipAnd(BlockParser.Instance);

        return Terms.Text("switch")
            .SkipAnd(Between(Terms.Char('('), ExpressionParser.Instance, Terms.Char(')')))
            .AndSkip(Terms.Char('{'))
            .And(ZeroOrMany(caseParser))
            .And(defaultParser)
            .AndSkip(Terms.Char('}'))
            .Then<Statement>(t => new SwitchStatement
            {
                Expression = t.Item1,
                Cases = t.Item2.ToList(),
                Default = t.Item3
            });
    }
}

