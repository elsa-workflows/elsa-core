namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class DefaultFileNameSanitizer : IFileNameSanitizer
{
    private static readonly char[] InvalidFileNameCharacters = Path.GetInvalidFileNameChars();

    /// <inheritdoc />
    public string Sanitize(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (!IsInvalidFileNameCharacter(value[i]))
                continue;

            return string.Create(value.Length, (value, i), static (buffer, state) =>
            {
                state.value.AsSpan(0, state.i).CopyTo(buffer);

                for (var j = state.i; j < state.value.Length; j++)
                {
                    var character = state.value[j];
                    buffer[j] = IsInvalidFileNameCharacter(character) ? '-' : character;
                }
            });
        }

        return value;
    }

    private static bool IsInvalidFileNameCharacter(char character) => character is '/' or '\\' || Array.IndexOf(InvalidFileNameCharacters, character) >= 0;
}

