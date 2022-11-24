using System;
using System.Reflection;
using Elsa.Builders;
using Elsa.Server.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.Version
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/version")]
    [Produces("application/json")]
    public class Get : Controller
    {
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public Get(IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerOperation(
            Summary = "Returns the current version.",
            Description = "Returns the current version.",
            OperationId = "Version.Get",
            Tags = new[] { "Version" })
        ]
        public IActionResult Handle()
        {
            var version = GetElsaVersionAsString();

            var model = new
            {
                Version = version
            };

            return Json(model, _serializerSettingsProvider.GetSettings());
        }

        private static string GetElsaVersionAsString()
        {
            var elsaAssembly = typeof(IWorkflow).Assembly; // Assembly 'Elsa.Abstractions'.
            var attribute = elsaAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!;
            return GetVersionWithoutSuffix(attribute.InformationalVersion);
        }

        private static string GetVersionWithoutSuffix(string version)
        {
            var position = version.IndexOf('+', StringComparison.Ordinal);
            return position == -1 ? version : version[..position];
        }
    }
}