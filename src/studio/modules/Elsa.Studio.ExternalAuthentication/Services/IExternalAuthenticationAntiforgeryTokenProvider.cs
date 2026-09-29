namespace Elsa.Studio.ExternalAuthentication.Services;

/// <summary>Supplies the anti-forgery field for server-hosted broker forms without coupling shared UI to ASP.NET Core MVC.</summary>
public interface IExternalAuthenticationAntiforgeryTokenProvider
{
    ExternalAuthenticationAntiforgeryToken? GetToken();
}

/// <summary>A form field and request token generated for the current server request.</summary>
public sealed record ExternalAuthenticationAntiforgeryToken(string FormFieldName, string RequestToken);
