using Antlr4.Runtime;
using Elsa.Dsl.Contracts;
using Elsa.Dsl.Interpreters;
using Elsa.Dsl.Models;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Dsl.Services;

/// <inheritdoc />
public class DslEngine : IDslEngine
{
    private readonly ITypeSystem _typeSystem;
    private readonly IFunctionActivityRegistry _functionActivityRegistry;
    private readonly IExpressionHandlerRegistry _expressionHandlerRegistry;
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DslEngine"/> class.
    /// </summary>
    public DslEngine(
        ITypeSystem typeSystem,
        IFunctionActivityRegistry functionActivityRegistry,
        IExpressionHandlerRegistry expressionHandlerRegistry,
        IWorkflowBuilderFactory workflowBuilderFactory)
    {
        _typeSystem = typeSystem;
        _functionActivityRegistry = functionActivityRegistry;
        _expressionHandlerRegistry = expressionHandlerRegistry;
        _workflowBuilderFactory = workflowBuilderFactory;
    }

    /// <inheritdoc />
    public async Task<Workflow> ParseAsync(string script, CancellationToken cancellationToken = default)
    {
        var stream = CharStreams.fromString(script);
        var lexer = new ElsaLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new ElsaParser(tokens);
        var tree = parser.program();
        var interpreter = new WorkflowDefinitionBuilderInterpreter(_typeSystem, _functionActivityRegistry, _expressionHandlerRegistry, _workflowBuilderFactory, new WorkflowDefinitionInterpreterSettings());
        var workflowBuilder = interpreter.Visit(tree);
        var workflow = await workflowBuilder.BuildWorkflowAsync(cancellationToken);

        return workflow;
    }
}