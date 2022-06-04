using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Models;

public class ExpressionExecutionContext
{
    private readonly IServiceProvider _serviceProvider;

    public ExpressionExecutionContext(
        IServiceProvider serviceProvider,
        MemoryRegister memoryRegister,
        ExpressionExecutionContext? parentContext = default,
        IDictionary<object, object>? applicationProperties = default,
        CancellationToken cancellationToken = default)
    {
        _serviceProvider = serviceProvider;
        MemoryRegister = memoryRegister;
        ApplicationProperties = applicationProperties ?? new Dictionary<object, object>();
        ParentContext = parentContext;

        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// A shared register of computer memory. 
    /// </summary>
    public MemoryRegister MemoryRegister { get; }

    public IDictionary<object, object> ApplicationProperties { get; set; }
    public ExpressionExecutionContext? ParentContext { get; set; }
    public CancellationToken CancellationToken { get; }

    public MemoryDatum GetDatum(MemoryDatumReference reference) => GetMemoryDatumInternal(reference) ?? throw new InvalidOperationException();
    public object Get(MemoryDatumReference reference) => GetDatum(reference).Value!;
    public T Get<T>(MemoryDatumReference reference) => (T)Get(reference);

    public void Set(MemoryDatumReference reference, object? value)
    {
        var datum = GetMemoryDatumInternal(reference) ?? MemoryRegister.Declare(reference);
        datum.Value = value;
    }

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    //private MemoryDatum? GetMemoryDatumInternal(MemoryDatumReference locationReference) => MemoryRegister.TryGetMemoryDatum(locationReference.Id, out var location) ? location : ParentContext?.GetMemoryDatumInternal(locationReference);
    private MemoryDatum? GetMemoryDatumInternal(MemoryDatumReference reference) => MemoryRegister.TryGetMemoryDatum(reference.Id, out var location) ? location : default;
}