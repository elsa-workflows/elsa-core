using Elsa.Common.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.UnitTests.Shared;
using Elsa.Workflows;
using FastEndpoints;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using CreateEndpoint = Elsa.Labels.Endpoints.Labels.Post.Create;
using CreateRequest = Elsa.Labels.Endpoints.Labels.Post.Request;
using GetEndpoint = Elsa.Labels.Endpoints.Labels.Get.Get;
using GetRequest = Elsa.Labels.Endpoints.Labels.Get.Request;
using UpdateEndpoint = Elsa.Labels.Endpoints.Labels.Update.Update;
using UpdateRequest = Elsa.Labels.Endpoints.Labels.Update.Request;

namespace Elsa.Labels.UnitTests.Endpoints;

/// <summary>
/// The label create, get and update endpoints return the display name next to the normalized name, so clients keep its casing (#8189).
/// </summary>
[Collection(nameof(FastEndpointsCollection))]
public class LabelEndpointsResponseTests
{
    private readonly InMemoryLabelStore _store = new(new MemoryStore<Label>(), new MemoryStore<WorkflowDefinitionLabel>(), new TestTenantAccessor("tenant-a"));

    [Fact]
    public async Task Create_ReturnsDisplayName()
    {
        var identityGenerator = Substitute.For<IIdentityGenerator>();
        identityGenerator.GenerateId().Returns("label-1");
        var endpoint = Factory.Create<CreateEndpoint>(context =>
        {
            // The mapper resolves the identity generator, and CreatedAtAsync a link generator, from the request services.
            context.AddTestServices(services => services.AddSingleton(identityGenerator).AddSingleton(Substitute.For<LinkGenerator>()));
            context.Response.Body = new MemoryStream();
        }, _store, identityGenerator);

        await endpoint.HandleAsync(new CreateRequest { Name = "MyLabel" }, CancellationToken.None);

        Assert.Equal("MyLabel", endpoint.Response.Name);
        Assert.Equal("mylabel", endpoint.Response.NormalizedName);
    }

    [Fact]
    public async Task Get_ReturnsDisplayName()
    {
        await SaveLabelAsync("label-1", "MyLabel");
        var endpoint = Factory.Create<GetEndpoint>(context => context.Response.Body = new MemoryStream(), _store);

        await endpoint.HandleAsync(new GetRequest { Id = "label-1" }, CancellationToken.None);

        Assert.Equal("MyLabel", endpoint.Response.Name);
        Assert.Equal("mylabel", endpoint.Response.NormalizedName);
    }

    [Fact]
    public async Task Update_ReturnsDisplayName()
    {
        await SaveLabelAsync("label-1", "MyLabel");
        var endpoint = Factory.Create<UpdateEndpoint>(context => context.Response.Body = new MemoryStream(), _store);

        await endpoint.HandleAsync(new UpdateRequest { Id = "label-1", Name = "MyRenamedLabel" }, CancellationToken.None);

        Assert.Equal("MyRenamedLabel", endpoint.Response.Name);
        Assert.Equal("myrenamedlabel", endpoint.Response.NormalizedName);
    }

    private Task SaveLabelAsync(string id, string name) => _store.SaveAsync(new Label { Id = id, Name = name, TenantId = "tenant-a" });
}