using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Models;

/// <summary>
/// Made available to activities to access shared state and store local state. 
/// </summary>
public class ExpressionExecutionContext
{
    private readonly IServiceProvider _serviceProvider;

    public ExpressionExecutionContext(
        IServiceProvider serviceProvider,
        MemoryRegister memory,
        ExpressionExecutionContext? parentContext = default,
        IDictionary<object, object>? transientProperties = default,
        CancellationToken cancellationToken = default)
    {
        _serviceProvider = serviceProvider;
        Memory = memory;
        TransientProperties = transientProperties ?? new Dictionary<object, object>();
        ParentContext = parentContext;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// A shared register of computer memory. 
    /// </summary>
    public MemoryRegister Memory { get; }

    /// <summary>
    /// A dictionary of transient properties.
    /// </summary>
    public IDictionary<object, object> TransientProperties { get; set; }

    public ExpressionExecutionContext? ParentContext { get; set; }
    public CancellationToken CancellationToken { get; }

    public MemoryBlock GetBlock(MemoryBlockReference blockReference) => GetMemoryDatumInternal(blockReference) ?? throw new Exception($"Failed to retrieve memory block with reference {blockReference.Id}");
    public object Get(MemoryBlockReference blockReference) => GetBlock(blockReference).Value!;
    public T Get<T>(MemoryBlockReference blockReference) => (T)Get(blockReference);

    public void Set(MemoryBlockReference blockReference, object? value)
    {
        var datum = GetMemoryDatumInternal(blockReference) ?? Memory.Declare(blockReference);
        datum.Value = value;
    }

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    //private MemoryDatum? GetMemoryDatumInternal(MemoryDatumReference locationReference) => MemoryRegister.TryGetMemoryDatum(locationReference.Id, out var location) ? location : ParentContext?.GetMemoryDatumInternal(locationReference);
    private MemoryBlock? GetMemoryDatumInternal(MemoryBlockReference blockReference) => Memory.TryGetBlock(blockReference.Id, out var location) ? location : default;
}