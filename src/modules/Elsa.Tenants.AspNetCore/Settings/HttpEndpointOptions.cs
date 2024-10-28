namespace Elsa.Tenants.AspNetCore.Settings;

public class HttpEndpointSettings
{
    public string? Prefix { get; set; }
    public string? HeaderName { get; set; }
    public string? QueryStringName { get; set; }
    public string? Origin { get; set; }
}