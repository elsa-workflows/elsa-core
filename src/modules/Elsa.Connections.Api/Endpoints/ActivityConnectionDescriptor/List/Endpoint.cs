using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Nodes;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using FastEndpoints;
using Humanizer;

namespace Elsa.Connections.Api.Endpoints.ActivityConnectionDescriptor.List;


public class List(IConnectionDescriptorRegistry registry) : ElsaEndpointWithoutRequest<PagedListResponse<ConnectionDescriptor>>
{
    public override void Configure()
    {
        Get("/connection-configuration/descriptors");
        AllowAnonymous();
    }

    public override Task<PagedListResponse<ConnectionDescriptor>> ExecuteAsync(CancellationToken ct)
    {
        var descriptors = registry.ListAll().ToList();
        return  Task.FromResult(new PagedListResponse<ConnectionDescriptor>(Page.Of(descriptors, descriptors.Count())) );
    }
}