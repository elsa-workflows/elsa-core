namespace Elsa.Expressions.Models;

/// <summary>
/// A base class for types that represent a reference to a block of memory. 
/// </summary>
public abstract class MemoryBlockReference
{
    protected MemoryBlockReference()
    {
    }

    protected MemoryBlockReference(string id) => Id = id;

    public string Id { get; set; } = default!;
    public abstract MemoryBlock Declare();
    public object? Get(MemoryRegister memoryRegister) => GetBlock(memoryRegister).Value;
    public T? Get<T>(MemoryRegister memoryRegister) => (T?)Get(memoryRegister);
    public object? Get(ExpressionExecutionContext context) => context.Get(this);
    public T? Get<T>(ExpressionExecutionContext context) => (T?)Get(context);
    public void Set(MemoryRegister memoryRegister, object? value) => GetBlock(memoryRegister).Value = value;
    public void Set(ExpressionExecutionContext context, object? value) => context.Set(this, value);
    public MemoryBlock GetBlock(MemoryRegister memoryRegister) => memoryRegister.TryGetBlock(Id, out var location) ? location : memoryRegister.Declare(this);
}

public abstract class MemoryBlockReference<T> : MemoryBlockReference
{
}