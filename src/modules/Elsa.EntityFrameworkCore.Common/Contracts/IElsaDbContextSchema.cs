namespace Elsa.EntityFrameworkCore.Contracts;

/// <summary>
/// Interface to provide custom Elsa Db Context Schema in Migration
/// </summary>
public interface IElsaDbContextSchema
{
    /// <summary>
    /// Name of the Schema
    /// </summary>
    public string Schema { get; }
}
