using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace Elsa.Server.Api
{
    public class ElsaApiOptions
    {
        public Action<MvcNewtonsoftJsonOptions>? SetupNewtonsoftJson { get; set; } = default;
        public Action<ApiVersioningOptions>? ConfigureApiVersioningOptions { get; set; }
    }
}