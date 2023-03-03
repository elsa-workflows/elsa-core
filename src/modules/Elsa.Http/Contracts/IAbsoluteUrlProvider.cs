namespace Elsa.Http.Contracts;

public interface IAbsoluteUrlProvider
{
    Uri ToAbsoluteUrl(string relativePath);
}