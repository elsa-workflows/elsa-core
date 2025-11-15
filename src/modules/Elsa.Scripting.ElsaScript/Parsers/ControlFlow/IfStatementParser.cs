using Parlot;
using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using Elsa.Scripting.ElsaScript.Parsers.Containers;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers.ControlFlow;

public static class IfStatementParser
{
    public static readonly Parser<Statement> Instance = If();

    private static Parser<Statement> If()
    {
        // if '(' Condition ')' ThenPart [else ElsePart]
        return Terms.Text("if")
            .SkipAnd(Between(Terms.Char('('), Expressions.ExpressionParser.Instance, Terms.Char(')')))
            .And(StatementOrBlock())
            .And(ZeroOrOne(Terms.Text("else").SkipAnd(StatementOrBlock())))
            .Then<Statement>(t => new IfStatement
            {
                Condition = t.Item1,
                Then = t.Item2,
                Else = t.Item3
            });
    }

    // Parses either a single statement or a block { ... }
    private static Parser<StatementOrBlock> StatementOrBlock()
    {
        return BlockParser.Instance.Then(sb => new StatementOrBlock { Block = sb })
            .Or(StatementParser.Instance.Then<StatementOrBlock>(s => new StatementOrBlock { Statement = s }));
    }
}
