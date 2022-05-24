using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Models;

public class ExpressionExecutionContext
{
    private readonly IServiceProvider _serviceProvider;

    public ExpressionExecutionContext(
        IServiceProvider serviceProvider,
        Register register,
        Workflow workflow,
        IDictionary<string, object> input,
        IDictionary<object, object?> transientProperties,
        ExpressionExecutionContext? parentContext,
        CancellationToken cancellationToken)
    {
        _serviceProvider = serviceProvider;
        Register = register;
        Workflow = workflow;
        Input = input;
        TransientProperties = transientProperties;
        ParentContext = parentContext;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// A register of computer memory. 
    /// </summary>
    public Register Register { get; }
    
    public Workflow Workflow { get; }
    public IDictionary<string, object> Input { get; }
    public IDictionary<object, object?> TransientProperties { get; }
    public ExpressionExecutionContext? ParentContext { get; set; }
    public CancellationToken CancellationToken { get; }

    public RegisterLocation GetLocation(RegisterLocationReference locationReference) => GetLocationInternal(locationReference) ?? throw new InvalidOperationException();
    public object Get(RegisterLocationReference locationReference) => GetLocation(locationReference).Value!;
    public T Get<T>(RegisterLocationReference locationReference) => (T)Get(locationReference);
    public T? Get<T>(Input<T>? input) => input != null ? (T?)GetLocation(input.LocationReference).Value : default;
    public T? Get<T>(Output output) => (T?)GetLocation(output.LocationReference).Value;
    public object? Get(Output output) => GetLocation(output.LocationReference).Value;
    public T? GetVariable<T>(string name) => (T?)GetVariable(name);
    public T? GetVariable<T>() => (T?)GetVariable(typeof(T).Name);
    public object? GetVariable(string name) => new Variable(name).Get(this);

    public Variable SetVariable<T>(T? value) => SetVariable(typeof(T).Name, value);
    public Variable SetVariable<T>(string name, T? value) => SetVariable(name, (object?)value);

    public Variable SetVariable(string name, object? value)
    {
        var variable = new Variable(name, value);
        Set(variable, value);
        return variable;
    }

    public void Set(RegisterLocationReference locationReference, object? value)
    {
        var location = GetLocationInternal(locationReference) ?? Register.Declare(locationReference);
        location.Value = value;
    }

    public void Set(Output output, object? value)
    {
        //var convertedValue = output.ValueConverter?.Invoke(value) ?? value;
        var convertedValue = value;
        var targets = new[] { output.LocationReference }.Concat(output.Targets);
        foreach (var target in targets) Set(target, convertedValue);
    }

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();
    private RegisterLocation? GetLocationInternal(RegisterLocationReference locationReference) => Register.TryGetLocation(locationReference.Id, out var location) ? location : ParentContext?.GetLocationInternal(locationReference);
}