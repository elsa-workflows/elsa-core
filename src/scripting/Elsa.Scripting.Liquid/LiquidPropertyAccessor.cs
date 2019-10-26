﻿using System;
using System.Threading.Tasks;
using Fluid.Values;

namespace Elsa.Scripting.Liquid
{
    /// <summary>
    /// Can be used to provide a factory to return a value based on a property name 
    /// that is unknown at registration time. 
    /// 
    /// e.g. {{ LiquidPropertyAccessor.MyPropertyName }} (MyPropertyName will be passed as the identifier argument to the factory)
    /// </summary>
    public class LiquidPropertyAccessor
    {
        private readonly Func<string, Task<FluidValue>> getter;

        public LiquidPropertyAccessor(Func<string, Task<FluidValue>> getter)
        {
            this.getter = getter;
        }
        public Task<FluidValue> GetValueAsync(string identifier)
        {
            return getter(identifier);
        }
    }
}