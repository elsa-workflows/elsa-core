using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Elsa.Secrets.Models;
using Elsa.Secrets.ValueFormatters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Elsa.Secrets.ValueFormatters {
    public class OAuth2SecretValueFormatter : ISecretValueFormatter
    {
        private readonly ILogger<OAuth2SecretValueFormatter> _logger;
        private HttpClient _httpClient;
        public string Type => "OAuth2";

        public OAuth2SecretValueFormatter(IHttpClientFactory httpClientFactory, ILogger<OAuth2SecretValueFormatter> logger) {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient(nameof(OAuth2SecretValueFormatter));
        }

        public async Task<string> FormatSecretValue(Secret secret) {
            var content = new Dictionary<string, string> {
                {"grant_type", "client_credentials"},
                {"client_id", secret.GetProperty("ClientId")},
                {"client_secret", secret.GetProperty("ClientSecret")},
                {"scope", secret.GetProperty("Scope")}
            };

            try {
                var response = await _httpClient.PostAsync(secret.GetProperty("AccessTokenUrl"), new FormUrlEncodedContent(content));
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TokenResponse>(json);
                if (string.IsNullOrEmpty(result?.AccessToken)) {
                    throw new Exception(result?.Error ?? "Failed to obtain OAuth2 token");
                }
                return result.AccessToken;
            }
            catch (Exception e) {
                _logger.LogError(e, $"Failed to obtain OAuth2 token for secret {secret.Name}/{secret.Id}");
                throw;
            }
        }
    }
}