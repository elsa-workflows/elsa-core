using System.Diagnostics.CodeAnalysis;
using Elsa.Api.Client.Resources.OutputConverters.Contracts;
using Elsa.Api.Client.Resources.OutputConverters.Models;
using Elsa.Api.Client.Resources.OutputConverters.Requests;
using Elsa.Api.Client.Resources.OutputConverters.Responses;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests.OutputConverters;

public class RemoteOutputConverterServiceTests
{
    [Fact]
    public async Task GetOutputConvertersAsyncForwardsDeclaredTypesToTheBackend()
    {
        var api = new OutputConvertersApiStub();
        var service = new RemoteOutputConverterService(new BackendApiClientProviderStub(api));

        var converters = await service.GetOutputConvertersAsync("Source", "String");

        Assert.Equal("Source", api.Request!.SourceType);
        Assert.Equal("String", api.Request.DestinationType);
        Assert.Single(converters);
        Assert.Equal("sample.to-text", converters.Single().Id);
    }

    private sealed class OutputConvertersApiStub : IOutputConvertersApi
    {
        public ListOutputConvertersRequest? Request { get; private set; }

        public Task<ListOutputConvertersResponse> ListAsync(ListOutputConvertersRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new ListOutputConvertersResponse(
            [
                new OutputConverterDescriptor
                {
                    Id = "sample.to-text",
                    SourceTypeName = "Source",
                    ResultTypeName = "String",
                    DisplayName = "Convert to text"
                }
            ]));
        }
    }

    private sealed class BackendApiClientProviderStub(IOutputConvertersApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://studio.test");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class =>
            typeof(T) == typeof(IOutputConvertersApi)
                ? ValueTask.FromResult((T)api)
                : throw new NotSupportedException();
    }
}
