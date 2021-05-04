namespace Elsa.Models
{
    public interface ITenantScope
    {
        string? TenantId { get; }
    }
}