using System;
using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using Elsa.Dsl.Models;
using Elsa.Dsl.Services;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Builders;
using Elsa.Workflows.Core.Services;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter : ElsaParserBaseVisitor<IWorkflowDefinitionBuilder>
{
    private readonly ITypeSystem _typeSystem;
    private readonly IFunctionActivityRegistry _functionActivityRegistry;
    private readonly IExpressionHandlerRegistry _expressionHandlerRegistry;
    private readonly IWorkflowDefinitionBuilder _workflowDefinitionBuilder;
    private readonly ParseTreeProperty<object> _object = new();
    private readonly ParseTreeProperty<object?> _expressionValue = new();
    private readonly ParseTreeProperty<IList<object?>> _argValues = new();
    private readonly ParseTreeProperty<Type> _expressionType = new();
    private readonly IDictionary<string, DefinedVariable> _definedVariables = new Dictionary<string, DefinedVariable>();
    private readonly Stack<IContainer> _containerStack = new();

    public WorkflowDefinitionBuilderInterpreter(
        ITypeSystem typeSystem, 
        IFunctionActivityRegistry functionActivityRegistry, 
        IExpressionHandlerRegistry expressionHandlerRegistry,
        IWorkflowDefinitionBuilderFactory workflowDefinitionBuilderFactory,
        WorkflowDefinitionInterpreterSettings settings)
    {
        _typeSystem = typeSystem;
        _functionActivityRegistry = functionActivityRegistry;
        _expressionHandlerRegistry = expressionHandlerRegistry;
        _workflowDefinitionBuilder = workflowDefinitionBuilderFactory.CreateBuilder();
    }

    protected override IWorkflowDefinitionBuilder DefaultResult => _workflowDefinitionBuilder;

    private void VisitMany(IEnumerable<IParseTree> contexts)
    {
        foreach (var parseTree in contexts) Visit(parseTree);
    }
}