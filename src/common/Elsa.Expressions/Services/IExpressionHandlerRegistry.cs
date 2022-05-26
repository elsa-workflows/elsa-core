namespace Elsa.Expressions.Services;

public interface IExpressionHandlerRegistry
{
    void Register(Type expression, Type handler);
    IExpressionHandler? GetHandler(IExpression input);
}