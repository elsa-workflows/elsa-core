using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Models;

public class ExpressionExecutionContext
{
    private readonly IServiceProvider _serviceProvider;

    public ExpressionExecutionContext(
        IServiceProvider serviceProvider,
        MemoryRegister memory,
        ExpressionExecutionContext? parentContext = default,
        IDictionary<object, object>? applicationProperties = default,
        CancellationToken cancellationToken = default)
    {
        _serviceProvider = serviceProvider;
        Memory = memory;
        ApplicationProperties = applicationProperties ?? new Dictionary<object, object>();
        ParentContext = parentContext;

        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// A shared register of computer memory. 
    /// </summary>
    public MemoryRegister Memory { get; }

    public IDictionary<object, object> ApplicationProperties { get; set; }
    public ExpressionExecutionContext? ParentContext { get; set; }
    public CancellationToken CancellationToken { get; }

    public MemoryBlock GetBlock(MemoryReference reference) => GetMemoryDatumInternal(reference) ?? throw new Exception($"Failed to retrieve memory block with reference {reference.Id}");
    public object Get(MemoryReference reference) => GetBlock(reference).Value!;
    public T Get<T>(MemoryReference reference) => (T)Get(reference);

    public void Set(MemoryReference reference, object? value)
    {
        var datum = GetMemoryDatumInternal(reference) ?? Memory.Declare(reference);
        datum.Value = value;
    }

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    //private MemoryDatum? GetMemoryDatumInternal(MemoryDatumReference locationReference) => MemoryRegister.TryGetMemoryDatum(locationReference.Id, out var location) ? location : ParentContext?.GetMemoryDatumInternal(locationReference);
    private MemoryBlock? GetMemoryDatumInternal(MemoryReference reference) => Memory.TryGetBlock(reference.Id, out var location) ? location : default;
}