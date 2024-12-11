using Elsa.Extensions;
using Elsa.Http;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Json.Path;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using Trimble.Elsa.Activities.Config;

namespace Trimble.Elsa.Activities.Activities;

/// <summary>
/// Base class for activities that authenticate with an HTTP request and store the token in the Token output.
/// Inheriting classes should specify the configuration object that will be used to make the request.
/// </summary>
public abstract class HttpRequestAuthenticator : SendHttpRequest
{
    public HttpRequestAuthenticator() { }

    /// <summary>
    /// Contains an encrypted token variable.
    /// </summary>
    public Output<string> Token { get; set; } = default!;

    protected abstract TokenRequestConfig? GetCredentials(ActivityExecutionContext context);
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var config = GetCredentials(context);

        if (config is null ||
            string.IsNullOrEmpty(config.TokenUrl) ||
            string.IsNullOrEmpty(config.ClientId) ||
            string.IsNullOrEmpty(config.ClientSecret))
        {
            context.LogError<HttpRequestAuthenticator>(
                "An empty config object was supplied to HttpRequestAuthenticator. Check your application config files, environment variables or startup configuration.",
                new
                {
                    type = GetType().Name
                });

            // Fault whole workflow if config is missing
            throw new ArgumentNullException("TokenRequestConfig object is null or missing required fields.");
        }

        context.LogDebug<HttpRequestAuthenticator>(
            "Executing HttpRequest to obtain a token.",
            new
            {
                type = GetType().Name,
                TokenUrl = config.TokenUrl,
                ClientId = config.ClientId,
            });

        Url = new(new Uri(config.TokenUrl));
        Method = new(HttpMethods.Post);
        ContentType = new("application/x-www-form-urlencoded");

        switch (config)
        {
            case PasswordGrantConfig passwordGrantConfig:
                var formData = new Dictionary<string, string>
                    {
                        { "grant_type", passwordGrantConfig.GrantType.ToString() },
                        { "client_id", passwordGrantConfig.ClientId },
                        { "client_secret",  passwordGrantConfig.ClientSecret },
                        { "username", passwordGrantConfig.Username },
                        { "password", passwordGrantConfig.Password + passwordGrantConfig.UserToken }
                    };
                Content = new(formData);
                break;
            case ClientCredentialsGrantConfig tokenRequestConfig:
                var clientCredsFormData = new Dictionary<string, string>
                    {
                        { "grant_type", tokenRequestConfig.GrantType.ToString() },
                        { "client_id", tokenRequestConfig.ClientId },
                        { "client_secret",  tokenRequestConfig.ClientSecret },
                        { "scope",  tokenRequestConfig.Scopes },
                    };
                Content = new(clientCredsFormData);
                break;
        }

        //ParsedContent must have a place to store its response or it will be null in HandleResponseAsync
        ParsedContent = new(new Variable<string>());

        await base.ExecuteAsync(context);

        // SendHttpRequestBase calls the following:
        //     context.Set(Result, response);
        //     context.Set(ParsedContent, parsedContent);
        //     context.Set(StatusCode, statusCode);
        //     context.Set(ResponseHeaders, responseHeaders);
    }

    /// <summary>
    /// Parses the token request and stored it in the Token output.
    /// </summary>
    protected override async ValueTask HandleResponseAsync(ActivityExecutionContext context, HttpResponseMessage response)
    {
        await base.HandleResponseAsync(context, response);

        var expandauBallet = context.ExpressionExecutionContext.Get(ParsedContent);
        var json = expandauBallet?.ToString();

        if (expandauBallet is IDictionary<string, object?> expandau)
        {
            //520905: Will not handle nested dictionaries because they will serialize into KeyValue pairs
            // rather than the original JSON schema.
            // There may be a way to retain this as a JSON string by reimplementing
            // SendHttpRequestBase::ParseContentAsync(ActivityExecutionContext context, HttpContent httpContent)
            json = JsonSerializer.Serialize(expandau);
        }

        if (json is not string)
        {
            context.LogCritical<HttpRequestAuthenticator>(
                "The token request response was not of the expected type",
                new
                {
                    type = expandauBallet?.GetType().Name
                });

            return;
        }

        string token = GetElementFromResponse(json, "$.access_token");
        string tokenType = GetElementFromResponse(json, "$.token_type");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(tokenType))
        {
            context.LogCritical<HttpRequestAuthenticator>("Token could not be found.");
        }

        // remove sensitive request data in case of serialization
        Content = default!;
        // remove sensitive result data in case of serialization
        ParsedContent = default!;

        context.SetOutputAndCheckEncryption(Token, $"{tokenType} {token}");
    }

    private string GetElementFromResponse(string json, string jsonPath)
    {
        var jsonNodes = JsonNode.Parse(json);
        var path = JsonPath.Parse(jsonPath);
        var pathResult = path.Evaluate(jsonNodes);
        return pathResult?.Matches?.FirstOrDefault()?.Value?.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Obtains a Salesforce password grant token
/// </summary>
[Activity("Trimble.Elsa.Activities.Activities", "ServiceRegistry", "Obtains a Salesforce password grant token", DisplayName = "Salesforce DX Authenticator", Kind = ActivityKind.Task)]
public class SalesforceAuthenticator : HttpRequestAuthenticator
{
    protected override TokenRequestConfig? GetCredentials(ActivityExecutionContext context)
    {
        return context.GetService<SalesforceDXTokenProvider>();
    }
}

/// <summary>
/// Obtains a Trimble ID client credentials token
/// </summary>
[Activity("Trimble.Elsa.Activities.Activities", "HTTP", "Obtains a Trimble ID client credentials token", DisplayName = "Trimble ID Authenticator", Kind = ActivityKind.Task)]
public class TrimbleIdAuthenticator : HttpRequestAuthenticator
{
    protected override TokenRequestConfig? GetCredentials(ActivityExecutionContext context)
    {
        return context.GetService<TrimbleIdTokenProvider>();
    }
}

/// <summary>
/// Obtains a ViewPoint ID client credentials token
/// </summary>
[Activity("Trimble.Elsa.Activities.Activities", "HTTP", "Obtains a ViewPoint ID client credentials token", DisplayName = "ViewPoint ID Authenticator", Kind = ActivityKind.Task)]
public class ViewpointIdAuthenticator : HttpRequestAuthenticator
{
    protected override TokenRequestConfig? GetCredentials(ActivityExecutionContext context)
    {
        return context.GetService<ViewpointIdTokenProvider>();
    }
}