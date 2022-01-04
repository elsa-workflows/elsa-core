namespace Elsa.Models;

public abstract class Argument
{
    protected Argument(RegisterLocationReference locationReference, Func<object?, object?>? valueConverter = default)
    {
        LocationReference = locationReference;
        ValueConverter = valueConverter;
    }

    public RegisterLocationReference LocationReference { get; set; }
    public Func<object?, object?>? ValueConverter { get; set; }
}