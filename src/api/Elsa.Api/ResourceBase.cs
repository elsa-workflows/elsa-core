using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Api;

public abstract class ResourceBase<T> where T:ResourceBase<T>
{
    protected ResourceBase(IEndpointRouteBuilder endpoints) => Endpoints = endpoints;
    protected IEndpointRouteBuilder Endpoints { get; }

    protected T MapGet(string pattern, Delegate handler)
    {
        Endpoints.MapGet(pattern, handler);
        return (T)this;
    }
    
    protected T MapPost(string pattern, Delegate handler)
    {
        Endpoints.MapPost(pattern, handler);
        return (T)this;
    }
}