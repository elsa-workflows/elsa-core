using Parlot.Fluent;
using Elsa.Scripting.ElsaScript.Ast;
using Elsa.Scripting.ElsaScript.Parsers.Containers;
using static Parlot.Fluent.Parsers;

namespace Elsa.Scripting.ElsaScript.Parsers.Metadata;

public static class WorkflowStatementParser
{
    public static readonly Parser<Statement> Instance = Workflow();

    private static Parser<Statement> Workflow()
    {
        // workflow "name"? { ... }
        // For now, just parse the name and body
        return Terms.Text("workflow")
            .SkipAnd(ZeroOrOne(Terms.String(StringLiteralQuotes.SingleOrDouble)))
            .AndSkip(Terms.Char('{'))
            .And(ZeroOrMany(StatementParser.Instance))
            .AndSkip(Terms.Char('}'))
            .Then(result =>
            {
                var name = result.Item1;
                var statements = result.Item2;
                var body = new Block { Statements = statements.ToList() };
                return new WorkflowStatement
                {
                    Name = name,
                    Body = body
                };
            });
    }
}

