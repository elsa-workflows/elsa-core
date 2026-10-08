namespace Elsa.Tenants.Mediator;

public static class TenantHeaders
{
    public static readonly object TenantIdKey = new();

    public static IDictionary<object, object> CreateHeaders(string? tenantId)
    {
        var headers = new Dictionary<object, object>();
        if (tenantId != null) headers.Add(TenantIdKey, tenantId);
        return headers;
    }
}