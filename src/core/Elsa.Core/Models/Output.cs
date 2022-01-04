namespace Elsa.Models;

public class Output : Argument
{
    public Output(RegisterLocationReference locationReference, Func<object?, object?>? valueConverter = default) : base(locationReference, valueConverter)
    {
    }
}

public class Output<T> : Output
{
    public Output(RegisterLocationReference locationReference, Func<object?, object?>? valueConverter = default) : base(locationReference, valueConverter)
    {
    }
}