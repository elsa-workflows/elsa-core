namespace Elsa.Models;

public abstract class RegisterLocationReference
{
    protected RegisterLocationReference()
    {
    }
        
    protected RegisterLocationReference(string id) => Id = id;

    public string Id { get; set; } = default!;
    public abstract RegisterLocation Declare();
    public object? Get(ExpressionExecutionContext context) => context.Get(this);
    public object? Get(ActivityExecutionContext context) => Get(context.ExpressionExecutionContext);
    public T? Get<T>(ExpressionExecutionContext context) => (T?)Get(context);
    public T? Get<T>(ActivityExecutionContext context) => (T?)Get(context.ExpressionExecutionContext);
    public void Set(ExpressionExecutionContext context, object? value) => context.Set(this, value);
    public void Set(ActivityExecutionContext context, object? value) => Set(context.ExpressionExecutionContext, value);

    public RegisterLocation GetLocation(Register register) => register.TryGetLocation(Id, out var location) ? location : register.Declare(this);
}