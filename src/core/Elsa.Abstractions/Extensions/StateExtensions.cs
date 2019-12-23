using System;
using System.Runtime.CompilerServices;
using Elsa.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Extensions
{
    public static class StateExtensions
    {
        public static T GetState<T>(this Variables state, [CallerMemberName]string key = null, Func<T> defaultValue = null)
        {
            if(!state.HasVariable(key))
                return defaultValue != null ? defaultValue() : default;
            
            return state.GetVariable<T>(key);
        }
        
        public static void SetState(this Variables state, object value, [CallerMemberName]string key = null) => state.SetVariable(key, value);
    }
}