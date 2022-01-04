using System;
using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using Elsa.Builders;
using Elsa.Contracts;
using Elsa.Dsl.Abstractions;
using Elsa.Dsl.Models;

namespace Elsa.Dsl.Interpreters;

public partial class WorkflowDefinitionBuilderInterpreter : ElsaParserBaseVisitor<IWorkflowDefinitionBuilder>
{
    private readonly ITypeSystem _typeSystem;
    private readonly IFunctionActivityRegistry _functionActivityRegistry;
    private readonly IExpressionHandlerRegistry _expressionHandlerRegistry;
    private readonly IWorkflowDefinitionBuilder _workflowDefinitionBuilder = new WorkflowDefinitionBuilder();
    private readonly ParseTreeProperty<object> _object = new();
    private readonly ParseTreeProperty<object?> _expressionValue = new();
    private readonly ParseTreeProperty<IList<object?>> _argValues = new();
    private readonly ParseTreeProperty<Type> _expressionType = new();
    private readonly IDictionary<string, DefinedVariable> _definedVariables = new Dictionary<string, DefinedVariable>();
    private readonly Stack<IContainer> _containerStack = new();

    public WorkflowDefinitionBuilderInterpreter(ITypeSystem typeSystem, IFunctionActivityRegistry functionActivityRegistry, IExpressionHandlerRegistry expressionHandlerRegistry, WorkflowDefinitionInterpreterSettings settings)
    {
        _typeSystem = typeSystem;
        _functionActivityRegistry = functionActivityRegistry;
        _expressionHandlerRegistry = expressionHandlerRegistry;
    }

    protected override IWorkflowDefinitionBuilder DefaultResult => _workflowDefinitionBuilder;

    private void VisitMany(IEnumerable<IParseTree> contexts)
    {
        foreach (var parseTree in contexts) Visit(parseTree);
    }
}