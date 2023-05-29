using System;
using Elsa.Secrets.Http.Models;

namespace Elsa.Secrets.Http.Extensions;

public static class TokenExtensions
{
	public static TokenData ToTokenData(this TokenResponse response)
	{
		return new TokenData
		{
			AccessToken = response.AccessToken,
			TokenType = response.TokenType,
			RefreshToken = response.RefreshToken,
			ExpiresAtUtc = DateTime.UtcNow.AddSeconds(response.ExpiresIn)
		};
	}
}