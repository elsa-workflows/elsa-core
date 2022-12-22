using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Models;

/// <summary>
/// Provides context to workflow expressions. 
/// </summary>
public class ExpressionExecutionContext
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
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

    /// <summary>
    /// Provides access to the parent <see cref="ExpressionExecutionContext"/>, if there is any.
    /// </summary>
    public ExpressionExecutionContext? ParentContext { get; set; }
    
    /// <summary>
    /// A cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Returns the <see cref="MemoryBlock"/> pointed to by the specified memory block reference.
    /// </summary>
    public MemoryBlock GetBlock(Func<MemoryBlockReference> blockReference) => GetBlock(blockReference());
    
    /// <summary>
    /// Returns the <see cref="MemoryBlock"/> pointed to by the specified memory block reference.
    /// </summary>
    public MemoryBlock GetBlock(MemoryBlockReference blockReference) => GetBlockInternal(blockReference) ?? throw new Exception($"Failed to retrieve memory block with reference {blockReference.Id}");
    
    public object Get(Func<MemoryBlockReference> blockReference) => Get(blockReference());
    public object Get(MemoryBlockReference blockReference) => GetBlock(blockReference).Value!;
    public T Get<T>(Func<MemoryBlockReference> blockReference) => Get<T>(blockReference());
    public T Get<T>(MemoryBlockReference blockReference) => (T)Get(blockReference);
    public void Set(Func<MemoryBlockReference> blockReference, object? value) => Set(blockReference(), value);

    public void Set(MemoryBlockReference blockReference, object? value)
    {
        var block = GetBlockInternal(blockReference) ?? Memory.Declare(blockReference);
        block.Value = value;
    }

    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();

    //private MemoryDatum? GetMemoryDatumInternal(MemoryDatumReference locationReference) => MemoryRegister.TryGetMemoryDatum(locationReference.Id, out var location) ? location : ParentContext?.GetMemoryDatumInternal(locationReference);
    private MemoryBlock? GetBlockInternal(MemoryBlockReference blockReference) => Memory.TryGetBlock(blockReference.Id, out var location) ? location : default;
}