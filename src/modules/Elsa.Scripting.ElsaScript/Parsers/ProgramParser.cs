using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers;

public static class ProgramParser
{
    public static readonly Parser<Program> Instance = Program();

    private static Parser<Program> Program()
    {
        return ZeroOrMany(StatementParser.Instance)
            .Then(stmts => new Program { Statements = stmts.ToList() });
    }
}

