namespace Elsa.Api.Client.Contracts;

/// <summary>
/// Provides an <see cref="HttpMessageHandler"/> instance.
/// </summary>
public interface IHttpMessageHandlerProvider
{
    /// <summary>
    /// Returns an <see cref="HttpMessageHandler"/> instance.
    /// </summary>
    HttpMessageHandler GetHandler();
}