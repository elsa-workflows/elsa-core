using System;

namespace Elsa.Secrets.Http.Models;

public class TokenData
{
	public string AccessToken { get; set; }
	public string? TokenType { get; set; }
	public string? RefreshToken { get; set; }
	public DateTime? ExpiresAtUtc { get; set; }
}