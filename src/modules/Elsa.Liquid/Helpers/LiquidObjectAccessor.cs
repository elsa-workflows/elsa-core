namespace Elsa.Liquid.Helpers;

/// <summary>
/// Can be used to provide a factory to return an object based on a property name that is unknown at registration time. 
/// </summary>
public class LiquidObjectAccessor<T>
{
    private readonly Func<string, Task<T>> _getter;
    public LiquidObjectAccessor(Func<string, Task<T>> getter) => _getter = getter;
    public Task<T> GetValueAsync(string identifier) => _getter(identifier);
}