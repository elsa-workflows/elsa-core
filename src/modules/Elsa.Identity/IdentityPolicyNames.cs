namespace Elsa.Identity;

/// <summary>
/// Provides policy name constants.
/// </summary>
public static class IdentityPolicyNames
{
    /// <summary>
    /// The Security Root policy represents root access to security related feature, such as hashing passwords. 
    /// </summary>
    [Obsolete("SecurityRoot is no longer used. See ADR 0010.")]
    public const string SecurityRoot = "SecurityRoot";
}