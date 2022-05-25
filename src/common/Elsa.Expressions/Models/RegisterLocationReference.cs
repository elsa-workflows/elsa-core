namespace Elsa.Expressions.Models;

public abstract class RegisterLocationReference
{
    protected RegisterLocationReference()
    {
    }
        
    protected RegisterLocationReference(string id) => Id = id;

    public string Id { get; set; } = default!;
    public abstract RegisterLocation Declare();
    public object? Get(Register register) => GetLocation(register).Value;
    public T? Get<T>(Register register) => (T?)Get(register);
    public object? Get(ExpressionExecutionContext context) => context.Get(this);
    public T? Get<T>(ExpressionExecutionContext context) => (T?)Get(context);
    public void Set(Register register, object? value) => GetLocation(register).Value = value;
    public void Set(ExpressionExecutionContext context, object? value) => context.Set(this, value);
    public RegisterLocation GetLocation(Register register) => register.TryGetLocation(Id, out var location) ? location : register.Declare(this);
}