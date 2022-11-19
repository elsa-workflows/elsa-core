namespace Elsa.Http.Services;

public interface IAbsoluteUrlProvider
{
    Uri ToAbsoluteUrl(string relativePath);
}