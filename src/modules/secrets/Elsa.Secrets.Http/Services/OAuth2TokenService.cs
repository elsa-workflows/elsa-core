using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Elsa.Secrets.Http.Extensions;
using Elsa.Secrets.Http.Models;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Elsa.Secrets.Http.Services;

public interface IOAuth2TokenService
{
	Task<TokenResponse> GetToken(Secret secret, string? authCode = null, string? redirectUri = null);
	Task<TokenResponse> GetTokenByRefreshToken(Secret secret, string refreshToken);
}

public class OAuth2TokenService : IOAuth2TokenService
{
	private readonly ILogger<OAuth2TokenService> _logger;
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ISecretsStore _secretsStore;

	public OAuth2TokenService(IHttpClientFactory httpClientFactory, ILogger<OAuth2TokenService> logger, ISecretsStore secretsStore)
	{
		_logger = logger;
		_httpClientFactory = httpClientFactory;
		_secretsStore = secretsStore;
	}
	
	private static string Base64Encode(string plainText) 
	{
		var bytes = Encoding.UTF8.GetBytes(plainText);
		return Convert.ToBase64String(bytes);
	}

	public async Task<TokenResponse> GetToken(Secret secret, string? authCode, string? redirectUri)
	{
		var clientId = secret.GetProperty("ClientId");
		var clientSecret = secret.GetProperty("ClientSecret");
		var clientAuthMethod = secret.GetProperty("ClientAuthMethod") ?? "client_secret_basic";
		var content = new Dictionary<string, string>
		{
			{ "grant_type", secret.GetProperty("GrantType") }
		};

		if (clientAuthMethod == "client_secret_post")
		{
			content.Add("client_id", clientId);
			if (!string.IsNullOrEmpty(clientSecret))
			{
				content.Add("client_secret", clientSecret);
			}
		}
		if (authCode != null)
		{
			content.Add("code", authCode);
		}
		else
		{
			content.Add("scope", secret.GetProperty("Scope"));
		}
		if (redirectUri != null)
		{
			content.Add("redirect_uri", redirectUri);
		}
		try
		{
			var httpClient = _httpClientFactory.CreateClient(nameof(OAuth2TokenService));
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			if (clientAuthMethod == "client_secret_basic")
			{
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", GenerateAuthorizationValue(clientId, clientSecret));
			}
			
			var response = await httpClient.PostAsync(secret.GetProperty("AccessTokenUrl"), new FormUrlEncodedContent(content));
			var json = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<TokenResponse>(json);

			if (string.IsNullOrEmpty(result?.AccessToken))
			{
				throw new Exception(result?.Error ?? "Failed to obtain OAuth2 token");
			}
			
			var tokenData = result.ToTokenData();
			secret.AddOrUpdateProperty("Token", JsonConvert.SerializeObject(tokenData));
			await _secretsStore.UpdateAsync(secret);

			return result;
		}
		catch (Exception e)
		{
			_logger.LogError(e, $"Failed to obtain OAuth2 token for secret {secret.Name}/{secret.Id}");
			throw;
		}
	}

	public async Task<TokenResponse> GetTokenByRefreshToken(Secret secret, string refreshToken)
	{
		var clientId = secret.GetProperty("ClientId");
		var clientSecret = secret.GetProperty("ClientSecret");
		var clientAuthMethod = secret.GetProperty("ClientAuthMethod") ?? "client_secret_basic";
		var content = new Dictionary<string, string>
		{
			{ "grant_type", "refresh_token" },
			{ "refresh_token", refreshToken }
		};
		
		if (clientAuthMethod == "client_secret_post")
		{
			content.Add("client_id", clientId);
			content.Add("client_secret", clientSecret);
		}

		try
		{
			var httpClient = _httpClientFactory.CreateClient(nameof(OAuth2TokenService));
			if (clientAuthMethod == "client_secret_basic")
			{
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", GenerateAuthorizationValue(clientId, clientSecret));
			}
			var response = await httpClient.PostAsync(secret.GetProperty("AccessTokenUrl"), new FormUrlEncodedContent(content));
			var json = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<TokenResponse>(json);
			
			if (response.StatusCode == HttpStatusCode.Unauthorized || !string.IsNullOrEmpty(result?.Error))
			{
				secret.RemoveProperty("Token");
				await _secretsStore.UpdateAsync(secret);
				throw new Exception("OAuth2 refresh token has expired - credential must be authorized with OAuth2 provider");
			}

			if (string.IsNullOrEmpty(result?.AccessToken))
			{
				throw new Exception(result?.Error ?? "Failed to obtain OAuth2 token");
			}

			var tokenData = result.ToTokenData();
			if (string.IsNullOrEmpty(tokenData.RefreshToken))
			{
				tokenData.RefreshToken = refreshToken;
			}
			secret.AddOrUpdateProperty("Token", JsonConvert.SerializeObject(tokenData));
			await _secretsStore.UpdateAsync(secret);

			return result;
		}
		catch (Exception e)
		{
			_logger.LogError(e, $"Failed to use refresh token to obtain OAuth2 token for secret {secret.Name}/{secret.Id}");
			throw;
		}
	}

	private static string GenerateAuthorizationValue(string clientId, string clientSecret)
	{
		var auth = Uri.EscapeDataString(clientId).Replace("%20", "+");
		if (!string.IsNullOrEmpty(clientSecret))
		{
			auth += $":{Uri.EscapeDataString(clientSecret).Replace("%20", "+")}";
		}
		return Base64Encode(auth);
	}
}