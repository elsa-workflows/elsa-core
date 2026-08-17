namespace Elsa.UserTasks.Options;

public sealed class UserTasksOptions
{
    public string DefaultTenantId { get; set; } = "";
    public string DefaultProvider { get; set; } = "default";
    public string SubjectClaimType { get; set; } = "sub";
    public string TenantClaimType { get; set; } = "tenant";
    public string ProviderClaimType { get; set; } = "elsa:identity-provider";
    public string DisplayNameClaimType { get; set; } = "name";
    public ICollection<string> GroupClaimTypes { get; set; } = ["groups", "group"];
    public ICollection<string> PermissionClaimTypes { get; set; } = ["permission", "permissions"];
    public int MaximumPayloadBytes { get; set; } = 256 * 1024;
    public TimeSpan DefaultInvitationLifetime { get; set; } = TimeSpan.FromDays(7);
}
