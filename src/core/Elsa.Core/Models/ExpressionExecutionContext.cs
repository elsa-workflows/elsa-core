namespace Elsa.Models;

public class ExpressionExecutionContext
{
    public ExpressionExecutionContext(Register register, ExpressionExecutionContext? parentContext)
    {
        Register = register;
        ParentContext = parentContext;
    }

    public Register Register { get; }
    public ExpressionExecutionContext? ParentContext { get; set; }

    public RegisterLocation GetLocation(RegisterLocationReference locationReference) => GetLocationInternal(locationReference) ?? throw new InvalidOperationException();
    public object Get(RegisterLocationReference locationReference) => GetLocation(locationReference).Value!;
    public T Get<T>(RegisterLocationReference locationReference) => (T)Get(locationReference);
    public T? Get<T>(Input<T> input) => (T?)GetLocation(input.LocationReference).Value;

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

    private RegisterLocation? GetLocationInternal(RegisterLocationReference locationReference) => Register.TryGetLocation(locationReference.Id, out var location) ? location : ParentContext?.GetLocationInternal(locationReference);
}