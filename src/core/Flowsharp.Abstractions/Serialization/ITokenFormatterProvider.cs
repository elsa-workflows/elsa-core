using Flowsharp.Serialization.Formatters;

namespace Flowsharp.Serialization
{
    public interface ITokenFormatterProvider
    {
        ITokenFormatter GetFormatter(string format);
    }
}