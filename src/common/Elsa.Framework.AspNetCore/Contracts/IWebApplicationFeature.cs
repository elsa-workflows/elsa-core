using Microsoft.AspNetCore.Builder;

namespace Elsa.Framework.AspNetCore;

public interface IWebApplicationFeature
{
    void Configure(IApplicationBuilder app);
}