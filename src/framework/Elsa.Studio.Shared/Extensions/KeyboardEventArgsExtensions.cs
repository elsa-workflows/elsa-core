using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Contains extension methods for <see cref="KeyboardEventArgs" />.
/// </summary>
public static class KeyboardEventArgsExtensions
{
    /// <summary>
    /// Determines whether the key submits a form: Enter, except when it confirms an IME composition (.NET 9 and later report that).
    /// </summary>
    public static bool IsSubmitKey(this KeyboardEventArgs e)
    {
#if NET9_0_OR_GREATER
        if (e.IsComposing)
            return false;
#endif
        return e.Key == "Enter";
    }
}
