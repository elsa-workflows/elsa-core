using System;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Expressions.Models;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Expressions;

namespace Elsa.Workflows.Core.Models;

public abstract class Input : Argument
{
    protected Input(IExpression expression, MemoryBlockReference memoryBlockReference, Type type) : base(memoryBlockReference)
    {
        Expression = expression;
        Type = type;
    }

    public IExpression Expression { get; }
    public Type Type { get; set; }
}

public class Input<T> : Input
{
    public Input(T literal) : this(new Literal<T>(literal))
    {
    }

    public Input(Func<T> @delegate) : this(new DelegateBlockReference(() => @delegate()))
    {
    }

    public Input(Func<ExpressionExecutionContext, ValueTask<T?>> @delegate) : this(new DelegateBlockReference<T>(@delegate))
    {
    }
    
    public Input(Func<ValueTask<T?>> @delegate) : this(new DelegateBlockReference<T>(@delegate))
    {
    }
    
    public Input(Func<ExpressionExecutionContext, T> @delegate) : this(new DelegateBlockReference<T>(@delegate))
    {
    }

    public Input(Variable variable) : base(new VariableExpression(variable), variable, typeof(T))
    {
    }
    
    public Input(Output output) : base(new OutputExpression(output), output.MemoryBlockReference, typeof(T))
    {
    }

    public Input(Literal<T> literal) : base(new LiteralExpression(literal.Value), literal, typeof(T))
    {
    }
        
    public Input(Literal literal) : base(new LiteralExpression(literal.Value), literal, typeof(T))
    {
    }

    public Input(DelegateBlockReference delegateBlockReference) : base(new DelegateExpression(delegateBlockReference), delegateBlockReference, typeof(T))
    {
    }

    public Input(ElsaExpression expression) : this(new ElsaExpressionBlockReference(expression))
    {
    }

    public Input(IExpression expression, MemoryBlockReference memoryBlockReference) : base(expression, memoryBlockReference, typeof(T))
    {
    }

    private Input(ElsaExpressionBlockReference expressionBlockReference) : base(expressionBlockReference.Expression, expressionBlockReference, typeof(T))
    {
    }
}