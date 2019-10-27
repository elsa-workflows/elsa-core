using System;
using System.Threading.Tasks;

namespace Elsa.Scripting.Liquid.Helpers
{
    /// <summary>
    /// Can be used to provide a factory to return an object based on a property name that is unknown at registration time. 
    /// </summary>
    public class LiquidObjectAccessor<T>
    {
        private readonly Func<string, Task<T>> getter;

        public LiquidObjectAccessor(Func<string, Task<T>> getter)
        {
            this.getter = getter;
        }

        public Task<T> GetValueAsync(string identifier)
        {
            return getter(identifier);
        }
    }
}