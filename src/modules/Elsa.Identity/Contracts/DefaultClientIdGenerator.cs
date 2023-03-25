using System.Text;

namespace Elsa.Identity.Contracts;

/// <inheritdoc />
public class DefaultClientIdGenerator : IClientIdGenerator
{
    private const int Length = 16;
    private static readonly char[] Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultClientIdGenerator"/> class.
    /// </summary>
    public DefaultClientIdGenerator()
    {
        _random = new Random();
    }

    /// <inheritdoc />
    public string Generate()
    {
        var identifierBuilder = new StringBuilder(8);

        for (var i = 0; i < Length; i++)
        {
            var randomIndex = _random.Next(Chars.Length);
            identifierBuilder.Append(Chars[randomIndex]);
        }

        return identifierBuilder.ToString();
    }
}