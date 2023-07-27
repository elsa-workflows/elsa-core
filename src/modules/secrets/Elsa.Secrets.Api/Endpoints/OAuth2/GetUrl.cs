using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Elsa.Secrets.Api.Endpoints.OAuth2;

[ApiController]
[ApiVersion("1")]
[Route("v{apiVersion:apiVersion}/oauth2/url/{secretId}")]
[Produces(MediaTypeNames.Application.Json)]
public class GetUrl : Controller
{
	private readonly ISecretsStore _secretStore;
	private readonly IConfiguration _configuration;

	public GetUrl(ISecretsStore secretStore, IConfiguration configuration)
	{ 
		_secretStore = secretStore;
		_configuration = configuration;
	}

	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
	public async Task<ActionResult<IEnumerable<Secret>>> Handle(string secretId, CancellationToken cancellationToken = default)
	{
		var secret = await _secretStore.FindByIdAsync(secretId, cancellationToken);
		if (secret == null)
			return NotFound();

		var uriBuilder = new UriBuilder(secret.GetProperty("AuthorizationUrl"));
		var query = HttpUtility.ParseQueryString(uriBuilder.Query);
		query["response_type"] = "code";
		query["client_id"] = secret.GetProperty("ClientId");
		query["scope"] = secret.GetProperty("Scope");
		query["prompt"] = "consent";
		query["access_type"] = "offline";
		query["state"] = secret.Id;
		query["redirect_uri"] = $"{_configuration["Elsa:Server:BaseUrl"]}/v1/oauth2/callback";
		uriBuilder.Query = query.ToString();

		return Ok(uriBuilder.ToString());
	}
}