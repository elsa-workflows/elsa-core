using System;
using System.Collections.Generic;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EndpointsAttribute : Attribute
    {
        public EndpointsAttribute(params string[] endpoints)
        {
            Endpoints = endpoints;
        }
        
        public IReadOnlyCollection<string> Endpoints { get; }
    }
}