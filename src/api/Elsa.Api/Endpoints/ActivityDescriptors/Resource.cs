using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Api.Endpoints.ActivityDescriptors;

public class ActivityDescriptorsResource
{
    private readonly IEndpointRouteBuilder _endpoints;

    public ActivityDescriptorsResource(IEndpointRouteBuilder endpoints)
    {
        _endpoints = endpoints;
    }

    public ActivityDescriptorsResource List()
    {
        _endpoints.MapGet("api/descriptors/activities", ActivityDescriptors.ListAsync);
        return this;
    }
}

public static class ActivityDescriptorsResourceExtensions
{
    public static IEndpointRouteBuilder MapActivityDescriptors(this IEndpointRouteBuilder endpoints, Action<ActivityDescriptorsResource> configureResource)
    {
        configureResource(new(endpoints));
        return endpoints;
    }

    public static IEndpointRouteBuilder MapActivityDescriptors(this IEndpointRouteBuilder endpoints) => endpoints.MapActivityDescriptors(x => x.List());
}