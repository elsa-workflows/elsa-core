using System;
using System.Runtime.CompilerServices;
using Elsa.Models;

namespace Elsa.Extensions
{
    public static class StateExtensions
    {
        public static T GetState<T>(this Variables state, [CallerMemberName]string? key = null, Func<T>? defaultValue = null)
        {
            if (!state.HasVariable(key))
            {
                var value = defaultValue != null ? defaultValue() : default;
                state.SetVariable(key, value);
            }
            
            return state.GetVariable<T>(key);
        }
        
        public static void SetState(this Variables state, object value, [CallerMemberName]string? key = null) => state.SetVariable(key, value);
    }
}