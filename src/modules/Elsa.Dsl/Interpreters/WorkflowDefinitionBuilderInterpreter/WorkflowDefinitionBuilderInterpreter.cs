using Antlr4.Runtime.Tree;
using Elsa.Dsl.Contracts;
using Elsa.Dsl.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter : ElsaParserBaseVisitor<IWorkflowBuilder>
{
    private readonly ITypeSystem _typeSystem;
    private readonly IActivityRegistry _activityRegistry;
    private readonly IFunctionActivityRegistry _functionActivityRegistry;
    private readonly IWorkflowBuilder _workflowBuilder;
    private readonly ParseTreeProperty<object> _object = new();
    private readonly ParseTreeProperty<object?> _expressionValue = new();
    private readonly ParseTreeProperty<IList<object?>> _argValues = new();
    private readonly ParseTreeProperty<Type> _expressionType = new();
    private readonly IDictionary<string, DefinedVariable> _definedVariables = new Dictionary<string, DefinedVariable>();
    private readonly Stack<Container> _containerStack = new();

    /// <inheritdoc />
    public WorkflowDefinitionBuilderInterpreter(
        ITypeSystem typeSystem,
        IActivityRegistry activityRegistry,
        IFunctionActivityRegistry functionActivityRegistry,
        IWorkflowBuilderFactory workflowBuilderFactory)
    {
        _typeSystem = typeSystem;
        _activityRegistry = activityRegistry;
        _functionActivityRegistry = functionActivityRegistry;
        _workflowBuilder = workflowBuilderFactory.CreateBuilder();
    }

    /// <inheritdoc />
    protected override IWorkflowBuilder DefaultResult => _workflowBuilder;
}