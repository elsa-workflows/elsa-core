namespace Elsa.Expressions.Models;

/// <summary>
/// A base class for types that represent a reference to a block of memory. 
/// </summary>
public abstract class MemoryReference
{
    protected MemoryReference()
    {
        //Id = Guid.NewGuid().ToString("N");
    }
        
    protected MemoryReference(string id) => Id = id;

    public string Id { get; set; } = default!;
    public abstract MemoryBlock Declare();
    public object? Get(MemoryRegister memoryRegister) => GetLocation(memoryRegister).Value;
    public T? Get<T>(MemoryRegister memoryRegister) => (T?)Get(memoryRegister);
    public object? Get(ExpressionExecutionContext context) => context.Get(this);
    public T? Get<T>(ExpressionExecutionContext context) => (T?)Get(context);
    public void Set(MemoryRegister memoryRegister, object? value) => GetLocation(memoryRegister).Value = value;
    public void Set(ExpressionExecutionContext context, object? value) => context.Set(this, value);
    public MemoryBlock GetLocation(MemoryRegister memoryRegister) => memoryRegister.TryGetMemoryDatum(Id, out var location) ? location : memoryRegister.Declare(this);
}