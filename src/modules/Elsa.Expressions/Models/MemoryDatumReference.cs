namespace Elsa.Expressions.Models;

public abstract class MemoryDatumReference
{
    protected MemoryDatumReference()
    {
    }
        
    protected MemoryDatumReference(string id) => Id = id;

    public string Id { get; set; } = default!;
    public abstract MemoryDatum Declare();
    public object? Get(MemoryRegister memoryRegister) => GetLocation(memoryRegister).Value;
    public T? Get<T>(MemoryRegister memoryRegister) => (T?)Get(memoryRegister);
    public object? Get(ExpressionExecutionContext context) => context.Get(this);
    public T? Get<T>(ExpressionExecutionContext context) => (T?)Get(context);
    public void Set(MemoryRegister memoryRegister, object? value) => GetLocation(memoryRegister).Value = value;
    public void Set(ExpressionExecutionContext context, object? value) => context.Set(this, value);
    public MemoryDatum GetLocation(MemoryRegister memoryRegister) => memoryRegister.TryGetMemoryDatum(Id, out var location) ? location : memoryRegister.Declare(this);
}