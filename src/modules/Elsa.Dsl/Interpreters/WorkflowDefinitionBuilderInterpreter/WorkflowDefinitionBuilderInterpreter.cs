using Antlr4.Runtime.Tree;
using Elsa.Dsl.Contracts;
using Elsa.Dsl.Models;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter : ElsaParserBaseVisitor<IWorkflowBuilder>
{
    private readonly ITypeSystem _typeSystem;
    private readonly IFunctionActivityRegistry _functionActivityRegistry;
    private readonly IExpressionHandlerRegistry _expressionHandlerRegistry;
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
        IFunctionActivityRegistry functionActivityRegistry, 
        IExpressionHandlerRegistry expressionHandlerRegistry,
        IWorkflowBuilderFactory workflowBuilderFactory,
        WorkflowDefinitionInterpreterSettings settings)
    {
        _typeSystem = typeSystem;
        _functionActivityRegistry = functionActivityRegistry;
        _expressionHandlerRegistry = expressionHandlerRegistry;
        _workflowBuilder = workflowBuilderFactory.CreateBuilder();
    }

    protected override IWorkflowBuilder DefaultResult => _workflowBuilder;

    private void VisitMany(IEnumerable<IParseTree> contexts)
    {
        foreach (var parseTree in contexts) Visit(parseTree);
    }
}