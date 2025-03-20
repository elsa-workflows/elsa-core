// using Elsa.Framework.Shells;
// using Elsa.Framework.Tenants;
// using Microsoft.AspNetCore.Http;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Elsa.Tenants.Middleware;
//
// public class TenantContainerMiddleware(RequestDelegate next, ITenantShellHost tenantShellHost)
// {
//     public async Task InvokeAsync(HttpContext httpContext, ITenantResolver tenantResolver)
//     {
//         var path = httpContext.Request.Path.Value;
//
//         // Extract tenant ID from the path
//         var segments = path?.Split('/', StringSplitOptions.RemoveEmptyEntries);
//         if (segments?.Length > 0)
//         {
//             var tenantId = segments[0];
//             httpContext.Request.PathBase = $"/{tenantId}";
//             httpContext.Items["TenantId"] = tenantId;
//             httpContext.Request.Path = new PathString("/" + string.Join("/", segments.Skip(1)));
//         }
//
//         // Resolve the tenant.
//         var currentTenant = await tenantResolver.GetTenantAsync();
//         var shell = tenantShellHost.GetShell(currentTenant.Id);
//         
//         // Replace the request services with the tenant-specific service provider.
//         var originalServiceProvider = httpContext.RequestServices;
//         await using var scope = shell.ServiceProvider.CreateAsyncScope();
//         httpContext.RequestServices = scope.ServiceProvider;
//         
//         // Call the next middleware in the pipeline.
//         await next(httpContext);
//         
//         // Restore the original service provider.
//         httpContext.RequestServices = originalServiceProvider;
//     }
// }