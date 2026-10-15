using Elsa.Common.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.UnitTests.Shared;
using Elsa.Workflows;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using CreateEndpoint = Elsa.Labels.Endpoints.Labels.Post.Create;
using CreateRequest = Elsa.Labels.Endpoints.Labels.Post.Request;
using UpdateEndpoint = Elsa.Labels.Endpoints.Labels.Update.Update;
using UpdateRequest = Elsa.Labels.Endpoints.Labels.Update.Request;

namespace Elsa.Labels.UnitTests.Endpoints;

/// <summary>
/// The label create and update endpoints return client errors for invalid names instead of 500 (#8756).
/// </summary>
[Collection(nameof(FastEndpointsCollection))]
public class LabelEndpointsNameValidationTests
{
    private readonly InMemoryLabelStore _store = new(new MemoryStore<Label>(), new MemoryStore<WorkflowDefinitionLabel>(), new TestTenantAccessor("tenant-a"));

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData(Label.NameMaxLength + 1)]
    public async Task Create_WithInvalidName_Returns400(object? name)
    {
        var endpoint = await CreateAsync(name is int length ? new string('a', length) : (string?)name);

        Assert.Equal(StatusCodes.Status400BadRequest, endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicateName_Returns409()
    {
        await SaveLabelAsync("label-1", "Foo");

        var endpoint = await CreateAsync(" FOO ");

        Assert.Equal(StatusCodes.Status409Conflict, endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Update_ToAnotherLabelsName_Returns409()
    {
        await SaveLabelAsync("label-1", "Foo");
        await SaveLabelAsync("label-2", "Bar");

        var endpoint = await UpdateAsync("label-2", "foo");

        Assert.Equal(StatusCodes.Status409Conflict, endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Update_WithTooLongName_Returns400()
    {
        await SaveLabelAsync("label-1", "Foo");

        var endpoint = await UpdateAsync("label-1", new string('a', Label.NameMaxLength + 1));

        Assert.Equal(StatusCodes.Status400BadRequest, endpoint.HttpContext.Response.StatusCode);
    }

    private async Task<CreateEndpoint> CreateAsync(string? name)
    {
        var endpoint = Factory.Create<CreateEndpoint>(context => context.Response.Body = new MemoryStream(), _store, Substitute.For<IIdentityGenerator>());
        await endpoint.HandleAsync(new CreateRequest { Name = name! }, CancellationToken.None);
        return endpoint;
    }

    private async Task<UpdateEndpoint> UpdateAsync(string id, string name)
    {
        var endpoint = Factory.Create<UpdateEndpoint>(context => context.Response.Body = new MemoryStream(), _store);
        await endpoint.HandleAsync(new UpdateRequest { Id = id, Name = name }, CancellationToken.None);
        return endpoint;
    }

    private Task SaveLabelAsync(string id, string name) => _store.SaveAsync(new Label { Id = id, Name = name, TenantId = "tenant-a" });
}
