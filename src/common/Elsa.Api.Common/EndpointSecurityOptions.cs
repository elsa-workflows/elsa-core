namespace Elsa;

public static class EndpointSecurityOptions
{
    public static string AdminRoleName = "Admin";
    public static string ReaderRoleName = "Reader";
    public static string WriteRoleName = "Writer";
    public static bool SecurityIsEnabled = true;

    public static void DisableSecurity() => SecurityIsEnabled = false;
}