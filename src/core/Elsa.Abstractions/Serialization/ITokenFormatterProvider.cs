using Elsa.Serialization.Formatters;

namespace Elsa.Serialization
{
    public interface ITokenFormatterProvider
    {
        ITokenFormatter GetFormatter(string format);
    }
}