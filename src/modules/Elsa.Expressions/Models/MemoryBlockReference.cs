using Elsa.Expressions.Helpers;

namespace Elsa.Expressions.Models;

/// <summary>
/// A base class for types that represent a reference to a block of memory. 
/// </summary>
public abstract class MemoryBlockReference
{
    /// <summary>
    /// Constructor.
    /// </summary>
    protected MemoryBlockReference()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    protected MemoryBlockReference(string id) => Id = id;

    /// <summary>
    /// The ID of the memory block.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Declares the memory block.
    /// </summary>
    public abstract MemoryBlock Declare();
    
    /// <summary>
    /// Returns true if the memory block is defined in the specified memory register.
    /// </summary>
    public bool IsDefined(MemoryRegister register) => register.HasBlock(Id);
    
    /// <summary>
    /// Returns the value of the memory block.
    /// </summary>
    public object? Get(MemoryRegister memoryRegister) => GetBlock(memoryRegister).Value;
    
    /// <summary>
    /// Returns the value of the memory block.
    /// </summary>
    public T? Get<T>(MemoryRegister memoryRegister) => Get(memoryRegister).ConvertTo<T>();
    
    /// <summary>
    /// Returns the value of the memory block.
    /// </summary>
    public object? Get(ExpressionExecutionContext context) => context.Get(this);
    
    /// <summary>
    /// Returns the value of the memory block.
    /// </summary>
    public T? Get<T>(ExpressionExecutionContext context) => Get(context).ConvertTo<T>();
    
    /// <summary>
    /// Returns the value of the memory block.
    /// </summary>
    public bool TryGet(ExpressionExecutionContext context, out object? value) => context.TryGet(this, out value);
    
    /// <summary>
    /// Sets the value of the memory block.
    /// </summary>
    public void Set(MemoryRegister memoryRegister, object? value, Action<MemoryBlock>? configure = default)
    {
        var block = GetBlock(memoryRegister);
        block.Value = value;
        configure?.Invoke(block);
    }

    /// <summary>
    /// Sets the value of the memory block.
    /// </summary>
    public void Set(ExpressionExecutionContext context, object? value, Action<MemoryBlock>? configure = default) => context.Set(this, value, configure);
    
    /// <summary>
    /// Returns the <see cref="MemoryBlock"/> pointed to by the specified memory block reference.
    /// </summary>
    public MemoryBlock GetBlock(MemoryRegister memoryRegister) => memoryRegister.TryGetBlock(Id, out var location) ? location : memoryRegister.Declare(this);
}

/// <summary>
/// A base class for types that represent a reference to a block of memory.
/// </summary>
/// <typeparam name="T">The type of the memory block.</typeparam>
public abstract class MemoryBlockReference<T> : MemoryBlockReference
{
}