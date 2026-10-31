namespace Elsa.Studio.Secrets;

/// <summary>Backend permission resources guarding the secrets APIs.</summary>
public static class SecretsPermissions
{
    /// <summary>Secret records.</summary>
    public const string Secrets = "secrets";
}

/// <summary>The secrets verbs beyond the core <c>PermissionVerbs</c>.</summary>
public static class SecretVerbs
{
    /// <summary>Resolve a secret to check that it works, without revealing its value.</summary>
    public const string Test = "test";
}
