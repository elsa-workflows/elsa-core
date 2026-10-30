using System.Diagnostics.CodeAnalysis;
using Elsa.Studio.Labels.Models;

namespace Elsa.Studio.Labels.Comparers;

/// <summary>
/// Compares two <see cref="Label"/> objects for equality based on their LabelId.
/// </summary>
public class LabelComparer : IEqualityComparer<Label>
{
    /// <summary>
    /// Determines whether the specified <see cref="Label"/> objects are equal.
    /// </summary>
    /// <param name="x">The first <see cref="Label"/> to compare.</param>
    /// <param name="y">The second <see cref="Label"/> to compare.</param>
    /// <returns><c>true</c> if the specified <see cref="Label"/> objects are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(Label? x, Label? y)
    {
        if (x is null && y is null)
        {
            return true;
        }

        return x?.Id == y?.Id;
    }

    /// <summary>
    /// Returns a hash code for the specified <see cref="Label"/>.
    /// </summary>
    /// <param name="obj">The <see cref="Label"/> for which a hash code is to be returned.</param>
    /// <returns>A hash code for the specified <see cref="Label"/>.</returns>
    public int GetHashCode([DisallowNull] Label obj)
    {
        return obj.Id.GetHashCode();
    }
}
