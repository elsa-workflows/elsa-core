using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Elsa.AllInOne.Web.Extensions;

public static class CorsOptionsExtensions
{
    private static bool AllowAny(IList<string> values) => values.Count == 0 || values[0] == "*";
    
    public static void Configure(this CorsOptions options, IConfigurationSection section)
    {
        var corsPolicy = section.Get<CorsPolicy>() ?? new CorsPolicy();
        options.AddDefaultPolicy(policy => {
            var headers = corsPolicy.Headers;
            var origins = corsPolicy.Origins;
            var methods = corsPolicy.Methods;

            if (AllowAny(headers))
                policy.AllowAnyHeader();
            else
                policy.WithHeaders(headers.ToArray());

            if (AllowAny(origins))
                policy.AllowAnyOrigin();
            else
                policy.WithOrigins(origins.ToArray());

            if (AllowAny(methods))
                policy.AllowAnyMethod();
            else
                policy.WithMethods(methods.ToArray());
        });
    }
}