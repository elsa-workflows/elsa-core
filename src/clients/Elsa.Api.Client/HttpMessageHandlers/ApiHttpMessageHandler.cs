using Elsa.Api.Client.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Api.Client.HttpMessageHandlers;

/// <summary>
/// An HTTP message handler that delegates its processing to another <see cref="HttpMessageHandler"/> instance provided by an <see cref="IHttpMessageHandlerProvider"/>.
/// </summary>
public class ApiHttpMessageHandler : DelegatingHandler
{
    /// <inheritdoc />
    public ApiHttpMessageHandler(IServiceProvider serviceProvider)
    {
        var provider = serviceProvider.GetService<IHttpMessageHandlerProvider>();

        if (provider != null)
            InnerHandler = provider.GetHandler();
    }
}