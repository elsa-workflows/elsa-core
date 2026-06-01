namespace Elsa.ModularPersistence.MongoDb.Options;

/// <summary>
/// Determines whether MongoDB write operations use driver transactions.
/// </summary>
public enum MongoDbTransactionMode
{
    /// <summary>
    /// Do not use MongoDB transactions. Single-document writes remain atomic.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Wrap each save and delete operation in a MongoDB transaction.
    /// </summary>
    TransactionPerWrite = 1
}
