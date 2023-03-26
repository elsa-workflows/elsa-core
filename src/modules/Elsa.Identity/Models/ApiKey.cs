using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;

namespace Elsa.Identity.Models;

/// <summary>
/// Represents a validated API key.
/// </summary>
/// <param name="Key">API Key</param>
/// <param name="OwnerName">Owner of the API Key. It can be username or any other key owner name.</param>
/// <param name="Claims">Optional list of claims to be sent back with the authentication request.</param>
public record ApiKey(string Key, string OwnerName, IReadOnlyCollection<Claim> Claims) : IApiKey;