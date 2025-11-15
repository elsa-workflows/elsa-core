using System.Reflection;
using Elsa.Dsl.ElsaScript.Ast;
using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Dsl.ElsaScript.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Dsl.ElsaScript.Compiler;

/// <summary>
/// Compiles an ElsaScript AST into an Elsa workflow.
/// </summary>
public class ElsaScriptCompiler(IActivityRegistryLookupService activityRegistryLookupService, IElsaScriptParser parser) : IElsaScriptCompiler
{
    private string _defaultExpressionLanguage = "JavaScript";
    private readonly Dictionary<string, Variable> _variables = new();

    /// <inheritdoc />
    public Task<Workflow> CompileAsync(string source, CancellationToken cancellationToken = default)
    {
        var workflowNode = parser.Parse(source);
        return CompileAsync(workflowNode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Workflow> CompileAsync(WorkflowNode workflowNode, CancellationToken cancellationToken = default)
    {
        _variables.Clear();

        // Process use statements
        foreach (var useNode in workflowNode.UseStatements)
        {
            if (useNode.Type == UseType.Expressions)
            {
                _defaultExpressionLanguage = MapLanguageName(useNode.Value);
            }
        }

        // Compile body statements
        var activities = new List<IActivity>();
        foreach (var statement in workflowNode.Body)
        {
            var activity = await CompileStatementAsync(statement, cancellationToken);
            if (activity != null)
            {
                activities.Add(activity);
            }
        }

        // Create the root activity (Sequence containing all statements)
        var root = activities.Count == 1
            ? activities[0]
            : new Sequence
            {
                Activities = activities
            };

        // Create the workflow
        var workflow = new Workflow
        {
            Name = workflowNode.Name,
            WorkflowMetadata = new(workflowNode.Name, workflowNode.Description, ToolVersion: new("3.6.0")),
            Root = root,
            Variables = _variables.Values.ToList()
        };

        return workflow;
    }

    private async Task<IActivity?> CompileStatementAsync(StatementNode statement, CancellationToken cancellationToken = default)
    {
        return statement switch
        {
            VariableDeclarationNode varDecl => CompileVariableDeclaration(varDecl),
            ActivityInvocationNode actInv => await CompileActivityInvocationAsync(actInv, cancellationToken),
            BlockNode block => await CompileBlockAsync(block, cancellationToken),
            IfNode ifNode => await CompileIfAsync(ifNode, cancellationToken),
            ForEachNode forEach => await CompileForEachAsync(forEach, cancellationToken),
            ForNode forNode => await CompileForAsync(forNode, cancellationToken),
            WhileNode whileNode => await CompileWhileAsync(whileNode, cancellationToken),
            SwitchNode switchNode => await CompileSwitchAsync(switchNode, cancellationToken),
            FlowchartNode flowchart => CompileFlowchart(flowchart),
            ListenNode listen => await CompileListenAsync(listen, cancellationToken),
            _ => throw new NotSupportedException($"Statement type {statement.GetType().Name} is not supported")
        };
    }

    private IActivity? CompileVariableDeclaration(VariableDeclarationNode varDecl)
    {
        // Create and register the variable
        var initialValue = varDecl.Value != null ? EvaluateConstantExpression(varDecl.Value) : null;
        var variable = new Variable(varDecl.Name, initialValue);
        _variables[varDecl.Name] = variable;

        // Variable declarations don't produce activities themselves
        return null;
    }

    private async Task<IActivity> CompileActivityInvocationAsync(ActivityInvocationNode actInv, CancellationToken cancellationToken = default)
    {
        // Try to find the activity type by name - try several strategies
        var activityDescriptor = await activityRegistryLookupService.FindAsync(actInv.ActivityName);

        // If not found, try with "Elsa." prefix
        if (activityDescriptor == null)
        {
            activityDescriptor = await activityRegistryLookupService.FindAsync($"Elsa.{actInv.ActivityName}");
        }

        // If still not found, search by descriptor name
        if (activityDescriptor == null)
        {
            activityDescriptor = await activityRegistryLookupService.FindAsync(d => d.Name == actInv.ActivityName);
        }

        if (activityDescriptor == null)
        {
            throw new InvalidOperationException($"Activity '{actInv.ActivityName}' not found in registry");
        }

        var activityType = activityDescriptor.ClrType;

        // Separate named and positional arguments
        var namedArgs = actInv.Arguments.Where(a => a.Name != null).ToList();
        var positionalArgs = actInv.Arguments.Where(a => a.Name == null).ToList();

        IActivity activity;

        // If we have positional arguments, try to find a matching constructor
        if (positionalArgs.Any())
        {
            activity = InstantiateActivityUsingConstructor(activityType, positionalArgs);
        }
        else
        {
            // No positional arguments, use default constructor
            var activityConstructorContext = new ActivityConstructorContext(activityDescriptor, ActivityActivator.Create);
            activity = activityDescriptor.Constructor(activityConstructorContext);
        }

        // Set named argument properties
        foreach (var arg in namedArgs)
        {
            var property = activityType.GetProperty(arg.Name!);

            if (property != null)
            {
                var value = CompileExpression(arg.Value, property.PropertyType);
                property.SetValue(activity, value);
            }
            else
            {
                throw new InvalidOperationException($"Property '{arg.Name}' not found on activity type '{activityType.Name}'");
            }
        }

        return activity;
    }

    private async Task<IActivity> CompileBlockAsync(BlockNode block, CancellationToken cancellationToken = default)
    {
        var activities = new List<IActivity>();

        foreach (var statement in block.Statements)
        {
            var activity = await CompileStatementAsync(statement, cancellationToken);
            if (activity != null)
            {
                activities.Add(activity);
            }
        }

        return new Sequence
        {
            Activities = activities
        };
    }

    private async Task<IActivity> CompileIfAsync(IfNode ifNode, CancellationToken cancellationToken = default)
    {
        var condition = CompileExpressionAsInput<bool>(ifNode.Condition);
        var thenActivity = await CompileStatementAsync(ifNode.Then, cancellationToken);
        var elseActivity = ifNode.Else != null ? await CompileStatementAsync(ifNode.Else, cancellationToken) : null;

        return new If(condition)
        {
            Then = thenActivity,
            Else = elseActivity
        };
    }

    private async Task<IActivity> CompileForEachAsync(ForEachNode forEach, CancellationToken cancellationToken = default)
    {
        // Create the loop variable
        var loopVariable = new Variable<object>(forEach.VariableName, null);
        _variables[forEach.VariableName] = loopVariable;

        var items = CompileExpressionAsInput<ICollection<object>>(forEach.Collection);
        var body = await CompileStatementAsync(forEach.Body, cancellationToken);

        var forEachActivity = new ForEach<object>(items)
        {
            CurrentValue = new(loopVariable),
            Body = body
        };

        return forEachActivity;
    }

    private async Task<IActivity> CompileForAsync(ForNode forNode, CancellationToken cancellationToken = default)
    {
        // For now, implement a simplified version using While
        // A full implementation would need to handle initializer and iterator properly
        throw new NotImplementedException("For loops are not yet fully implemented");
    }

    private async Task<IActivity> CompileWhileAsync(WhileNode whileNode, CancellationToken cancellationToken = default)
    {
        var condition = CompileExpressionAsInput<bool>(whileNode.Condition);
        var body = await CompileStatementAsync(whileNode.Body, cancellationToken);

        return new While(condition)
        {
            Body = body
        };
    }

    private async Task<IActivity> CompileSwitchAsync(SwitchNode switchNode, CancellationToken cancellationToken = default)
    {
        var cases = new List<SwitchCase>();

        foreach (var caseNode in switchNode.Cases)
        {
            var caseExpression = CompileExpressionAsExpression(caseNode.Value);
            var caseBody = await CompileStatementAsync(caseNode.Body, cancellationToken);
            cases.Add(new("Case", caseExpression, caseBody!));
        }

        var defaultActivity = switchNode.Default != null ? await CompileStatementAsync(switchNode.Default, cancellationToken) : null;

        return new Switch
        {
            Cases = cases,
            Default = defaultActivity
        };
    }

    private IActivity CompileFlowchart(FlowchartNode flowchart)
    {
        throw new NotImplementedException("Flowchart support is not yet implemented");
    }

    private async Task<IActivity> CompileListenAsync(ListenNode listen, CancellationToken cancellationToken = default)
    {
        // Listen is just a regular activity invocation that can start a workflow
        var activity = await CompileActivityInvocationAsync(listen.Activity, cancellationToken);

        // Try to set CanStartWorkflow if the activity supports it
        var canStartWorkflowProp = activity.GetType().GetProperty("CanStartWorkflow");
        if (canStartWorkflowProp != null && canStartWorkflowProp.PropertyType == typeof(bool))
        {
            canStartWorkflowProp.SetValue(activity, true);
        }

        return activity;
    }

    private Input<T> CompileExpressionAsInput<T>(ExpressionNode exprNode)
    {
        if (exprNode is LiteralNode literal)
        {
            return new(new Literal(literal.Value!));
        }

        if (exprNode is IdentifierNode identifier)
        {
            // Reference to a variable
            if (_variables.TryGetValue(identifier.Name, out var variable))
            {
                return new(variable);
            }

            // If not found, treat as a literal
            return new(new Literal<T>(default!));
        }

        if (exprNode is ElsaExpressionNode elsaExpr)
        {
            var language = elsaExpr.Language != null ? MapLanguageName(elsaExpr.Language) : _defaultExpressionLanguage;
            var expression = new Expression(language, elsaExpr.Expression);
            return new(expression);
        }

        if (exprNode is ArrayLiteralNode arrayLiteral)
        {
            // For array literals, evaluate to a constant array if all elements are literals
            var elements = arrayLiteral.Elements.Select(EvaluateConstantExpression).ToArray();
            return new((T)(object)elements);
        }

        throw new NotSupportedException($"Expression type {exprNode.GetType().Name} is not supported");
    }

    private Expression CompileExpressionAsExpression(ExpressionNode exprNode)
    {
        if (exprNode is LiteralNode literal)
        {
            return Expression.LiteralExpression(literal.Value);
        }

        if (exprNode is ElsaExpressionNode elsaExpr)
        {
            var language = elsaExpr.Language != null ? MapLanguageName(elsaExpr.Language) : _defaultExpressionLanguage;
            return new(language, elsaExpr.Expression);
        }

        throw new NotSupportedException($"Expression type {exprNode.GetType().Name} is not supported as Expression");
    }

    private object CompileExpression(ExpressionNode exprNode, Type targetType)
    {
        // Check if targetType is already Input<T>
        Type innerType;
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Input<>))
        {
            // Extract the T from Input<T>
            innerType = targetType.GetGenericArguments()[0];
        }
        else
        {
            innerType = targetType;
        }

        // Use reflection to call CompileExpressionAsInput<T>
        var method = GetType().GetMethod(nameof(CompileExpressionAsInput),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var genericMethod = method!.MakeGenericMethod(innerType);

        return genericMethod.Invoke(this, [exprNode])!;
    }

    private object? EvaluateConstantExpression(ExpressionNode exprNode)
    {
        if (exprNode is LiteralNode literal)
        {
            return literal.Value;
        }

        if (exprNode is ArrayLiteralNode arrayLiteral)
        {
            return arrayLiteral.Elements.Select(EvaluateConstantExpression).ToArray();
        }

        // For non-constant expressions, return null
        return null;
    }

    private IActivity InstantiateActivityUsingConstructor(Type activityType, List<ArgumentNode> positionalArgs)
    {
        // Get all public constructors
        var constructors = activityType.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Filter constructors that:
        // 1. Have the same number of required Input<T> parameters as positional arguments (excluding optional params)
        // 2. All non-optional parameters are Input<T> types
        var matchingConstructors = new List<(System.Reflection.ConstructorInfo ctor, System.Reflection.ParameterInfo[] inputParams)>();

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();

            // Filter to only Input<T> parameters that are not optional (don't have default values or CallerMemberName attributes)
            var inputParams = parameters.Where(p =>
                p.ParameterType.IsGenericType &&
                p.ParameterType.GetGenericTypeDefinition() == typeof(Input<>) &&
                !p.IsOptional &&
                !p.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CallerFilePathAttribute), false).Any() &&
                !p.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CallerLineNumberAttribute), false).Any() &&
                !p.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CallerMemberNameAttribute), false).Any()
            ).ToArray();

            // Check if the number of required Input<T> params matches our positional args
            if (inputParams.Length == positionalArgs.Count)
            {
                matchingConstructors.Add((ctor, inputParams));
            }
        }

        if (!matchingConstructors.Any())
        {
            throw new InvalidOperationException(
                $"No matching constructor found for activity type '{activityType.Name}' with {positionalArgs.Count} positional argument(s). " +
                $"Constructors must have Input<T> parameters matching the number of positional arguments.");
        }

        if (matchingConstructors.Count > 1)
        {
            throw new InvalidOperationException(
                $"Multiple matching constructors found for activity type '{activityType.Name}' with {positionalArgs.Count} positional argument(s). " +
                $"Please use named arguments to disambiguate.");
        }

        var (selectedCtor, selectedInputParams) = matchingConstructors[0];

        // Build the constructor arguments
        var ctorArgs = new List<object?>();
        var allParams = selectedCtor.GetParameters();

        foreach (var param in allParams)
        {
            // Check if this is one of our Input<T> parameters
            var inputParamIndex = Array.IndexOf(selectedInputParams, param);

            if (inputParamIndex >= 0)
            {
                // This is an Input<T> parameter - compile the corresponding positional argument
                var arg = positionalArgs[inputParamIndex];
                var value = CompileExpression(arg.Value, param.ParameterType);
                ctorArgs.Add(value);
            }
            else if (param.IsOptional)
            {
                // This is an optional parameter (like CallerFilePath) - use its default value
                ctorArgs.Add(param.DefaultValue);
            }
            else
            {
                // This shouldn't happen if our filtering is correct
                throw new InvalidOperationException(
                    $"Unexpected non-optional, non-Input<T> parameter '{param.Name}' in constructor for '{activityType.Name}'");
            }
        }

        // Instantiate the activity using the constructor
        var activity = (IActivity)selectedCtor.Invoke(ctorArgs.ToArray());
        return activity;
    }

    private string MapLanguageName(string dslLanguage)
    {
        return dslLanguage.ToLowerInvariant() switch
        {
            "js" => "JavaScript",
            "cs" => "C#",
            "py" => "Python",
            "liquid" => "Liquid",
            _ => dslLanguage
        };
    }
}