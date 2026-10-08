namespace Elsa.Identity.Constants;

/// <summary>
/// Provides a set of character sequences
/// </summary>
public static class CharacterSequences
{
    /// <summary>
    /// Gets an alphanumeric sequence of characters containing lowercase, uppercase and numerical characters.
    /// </summary>
    public static readonly char[] AlphanumericSequence = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
    
    /// <summary>
    /// Gets an alphanumeric sequence of characters containing lowercase, uppercase, numerical and symbol characters.
    /// </summary>
    public static readonly char[] AlphanumericAndSymbolSequence = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_-~/<>,.\\|';:\"".ToCharArray();
}