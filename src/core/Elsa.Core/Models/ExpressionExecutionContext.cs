using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Models;

public class ExpressionExecutionContext
{
    private readonly IServiceProvider _serviceProvider;

    public ExpressionExecutionContext(IServiceProvider serviceProvider, Register register, ExpressionExecutionContext? parentContext, CancellationToken cancellationToken)
    {
        _serviceProvider = serviceProvider;
        Register = register;
        ParentContext = parentContext;
        CancellationToken = cancellationToken;
    }

    public Register Register { get; }
    public ExpressionExecutionContext? ParentContext { get; set; }
    public CancellationToken CancellationToken { get; }

    public RegisterLocation GetLocation(RegisterLocationReference locationReference) => GetLocationInternal(locationReference) ?? throw new InvalidOperationException();
    public object Get(RegisterLocationReference locationReference) => GetLocation(locationReference).Value!;
    public T Get<T>(RegisterLocationReference locationReference) => (T)Get(locationReference);
    public T? Get<T>(Input<T>? input) => input != null ? (T?)GetLocation(input.LocationReference).Value : default;
    public T? GetVariable<T>(string name, object? defaultValue = default) => Get<T>(new Variable(name, defaultValue));

    public void Set(RegisterLocationReference locationReference, object? value)
    {
        var location = GetLocationInternal(locationReference) ?? Register.Declare(locationReference);
        location.Value = value;
    }

    public void Set(Output? output, object? value)
    {
        if (output?.LocationReference == null)
            return;

        var convertedValue = output.ValueConverter?.Invoke(value) ?? value;
        Set(output.LocationReference, convertedValue);
    }

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();
    private RegisterLocation? GetLocationInternal(RegisterLocationReference locationReference) => Register.TryGetLocation(locationReference.Id, out var location) ? location : ParentContext?.GetLocationInternal(locationReference);
}