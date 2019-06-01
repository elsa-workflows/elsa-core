using System;
using System.Collections.Generic;

namespace Elsa.Attributes
{
    public class EndpointsAttribute : Attribute
    {
        public EndpointsAttribute(params string[] endpoints)
        {
            Endpoints = endpoints;
        }
        
        public IReadOnlyCollection<string> Endpoints { get; }
    }
}