using Antlr4.Runtime;
using Elsa.Dsl.Interpreters;
using Elsa.Dsl.Models;
using Elsa.Dsl.Services;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Models;

namespace Elsa.Dsl.Implementations;

public class DslEngine : IDslEngine
{
    private readonly ITypeSystem _typeSystem;
    private readonly IFunctionActivityRegistry _functionActivityRegistry;
    private readonly IExpressionHandlerRegistry _expressionHandlerRegistry;

    public DslEngine(ITypeSystem typeSystem, IFunctionActivityRegistry functionActivityRegistry, IExpressionHandlerRegistry expressionHandlerRegistry)
    {
        _typeSystem = typeSystem;
        _functionActivityRegistry = functionActivityRegistry;
        _expressionHandlerRegistry = expressionHandlerRegistry;
    }

    public Workflow Parse(string script)
    {
        var stream = CharStreams.fromString(script);
        var lexer = new ElsaLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new ElsaParser(tokens);
        var tree = parser.program();
        var interpreter = new WorkflowDefinitionBuilderInterpreter(_typeSystem, _functionActivityRegistry, _expressionHandlerRegistry, new WorkflowDefinitionInterpreterSettings());
        var workflowBuilder = interpreter.Visit(tree);
        var workflow = workflowBuilder.BuildWorkflow();

        return workflow;
    }
}