namespace Elsa.Services
{
    /// <summary>
    /// Cone the specified source value.
    /// </summary>
    public interface ICloner
    {
        T Clone<T>(T source);
    }
}