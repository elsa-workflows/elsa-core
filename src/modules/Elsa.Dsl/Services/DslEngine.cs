using Antlr4.Runtime;
using Elsa.Dsl.Contracts;
using Elsa.Dsl.Interpreters;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Dsl.Services;

/// <inheritdoc />
public class DslEngine : IDslEngine
{
    private readonly ITypeSystem _typeSystem;
    private readonly IActivityRegistry _activityRegistry;
    private readonly IFunctionActivityRegistry _functionActivityRegistry;
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DslEngine"/> class.
    /// </summary>
    public DslEngine(
        ITypeSystem typeSystem,
        IActivityRegistry activityRegistry,
        IFunctionActivityRegistry functionActivityRegistry,
        IWorkflowBuilderFactory workflowBuilderFactory)
    {
        _typeSystem = typeSystem;
        _activityRegistry = activityRegistry;
        _functionActivityRegistry = functionActivityRegistry;
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

        var interpreter = new WorkflowDefinitionBuilderInterpreter(
            _typeSystem,
            _activityRegistry,
            _functionActivityRegistry,
            _workflowBuilderFactory);

        var workflowBuilder = interpreter.Visit(tree);
        var workflow = await workflowBuilder.BuildWorkflowAsync(cancellationToken);

        return workflow;
    }
}