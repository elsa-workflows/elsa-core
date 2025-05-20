using Antlr4.Runtime.Tree;
using Elsa.Scripting.Dsl.Contracts;
using Elsa.Scripting.Dsl.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Scripting.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter : ElsaParserBaseVisitor<IWorkflowBuilder>
{
    private readonly ITypeSystem _typeSystem;
    private readonly IActivityRegistryLookupService _activityRegistryLookup;
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
        IActivityRegistryLookupService activityRegistryLookup,
        IFunctionActivityRegistry functionActivityRegistry,
        IWorkflowBuilderFactory workflowBuilderFactory)
    {
        _typeSystem = typeSystem;
        _activityRegistryLookup = activityRegistryLookup;
        _functionActivityRegistry = functionActivityRegistry;
        _workflowBuilder = workflowBuilderFactory.CreateBuilder();
    }

    /// <inheritdoc />
    protected override IWorkflowBuilder DefaultResult => _workflowBuilder;
}