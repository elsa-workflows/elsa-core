namespace Elsa.Persistence.EFCore.MySql;

/// <summary>
/// MySQL-only stored-trigger unique index. Four <c>varchar(255)</c> utf8mb4 columns are 4,080 bytes,
/// above InnoDB's 3,072-byte unique-key limit; a 191-character prefix per column is 3,056 bytes.
/// Uniqueness is therefore enforced on the first 191 characters of each column, not the full 255.
/// That is enough for Elsa ids, hashes and tenant ids, which are far shorter than 191 characters.
/// The shared EF name is 69 characters and MySQL identifiers are capped at 64, so this truncated
/// name is what MySQL stored for the original four-column index.
/// </summary>
public static class StoredTriggerMySqlUniqueIndex
{
    public const string Name = "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_Ten";
    public const string ThreeColumnName = "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId";
    public const int PrefixLength = 191;
}
