using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Models;

public class ExpressionExecutionContext
{
    private readonly IServiceProvider _serviceProvider;

    public ExpressionExecutionContext(
        IServiceProvider serviceProvider,
        Register register,
        ExpressionExecutionContext? parentContext = default,
        IDictionary<object, object>? applicationProperties = default,
        CancellationToken cancellationToken = default)
    {
        _serviceProvider = serviceProvider;
        Register = register;
        ApplicationProperties = applicationProperties ?? new Dictionary<object, object>();
        ParentContext = parentContext;
        
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// A shared register of computer memory. 
    /// </summary>
    public Register Register { get; }

    public IDictionary<object, object> ApplicationProperties { get; set; }
    public ExpressionExecutionContext? ParentContext { get; set; }
    public CancellationToken CancellationToken { get; }

    public RegisterLocation GetLocation(RegisterLocationReference locationReference) => GetLocationInternal(locationReference) ?? throw new InvalidOperationException();
    public object Get(RegisterLocationReference locationReference) => GetLocation(locationReference).Value!;
    public T Get<T>(RegisterLocationReference locationReference) => (T)Get(locationReference);

    public void Set(RegisterLocationReference locationReference, object? value)
    {
        var location = GetLocationInternal(locationReference) ?? Register.Declare(locationReference);
        location.Value = value;
    }

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();
    private RegisterLocation? GetLocationInternal(RegisterLocationReference locationReference) => Register.TryGetLocation(locationReference.Id, out var location) ? location : ParentContext?.GetLocationInternal(locationReference);
}