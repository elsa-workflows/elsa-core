namespace Elsa.Services
{
    public interface ICloner
    {
        T Clone<T>(T source);
    }
}