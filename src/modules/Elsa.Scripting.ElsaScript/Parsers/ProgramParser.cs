using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using Elsa.Scripting.ElsaScript.Parsers.Metadata;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers;

public static class ProgramParser
{
    public static readonly Parser<Program> Instance = Program();

    private static Parser<Program> Program()
    {
        return ZeroOrOne(WorkflowStatementParser.Instance)
            .And(ZeroOrMany(StatementParser.Instance))
            .Then(result =>
            {
                var workflow = result.Item1 as WorkflowStatement;
                var statements = result.Item2;
                return new Program
                {
                    Workflow = workflow,
                    Statements = statements.ToList()
                };
            });
    }
}
