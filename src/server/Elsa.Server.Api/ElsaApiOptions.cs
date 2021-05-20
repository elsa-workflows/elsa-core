using System;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Server.Api
{
    public class ElsaApiOptions
    {
        public Action<MvcNewtonsoftJsonOptions>? SetupNewtonsoftJson { get; set; }
    }
}