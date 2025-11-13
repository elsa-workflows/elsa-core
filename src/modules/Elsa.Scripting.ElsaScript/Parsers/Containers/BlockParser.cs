using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers.Containers;

public static class BlockParser
{
    public static readonly Parser<Block> Instance = Block();

    private static Parser<Block> Block()
    {
        // '{' Statement* '}'
        return Terms.Char('{')
            .SkipAnd(ZeroOrMany(StatementParser.Instance))
            .AndSkip(Terms.Char('}'))
            .Then(stmts => new Block { Statements = stmts.ToList() });
    }
}

