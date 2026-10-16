namespace Elsa.Persistence.EFCore.MySql;

/// <summary>
/// MySQL-only stored-trigger unique index. Four <c>varchar(255)</c> utf8mb4 columns are 4,080 bytes,
/// above InnoDB's 3,072-byte unique-key limit; a 191-character prefix per column is 3,056 bytes.
/// The shared EF name is 69 characters and MySQL identifiers are capped at 64, so this truncated
/// name is what MySQL stored for the original four-column index.
/// </summary>
public static class StoredTriggerMySqlUniqueIndex
{
    public const string Name = "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_Ten";
    public const string ThreeColumnName = "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId";
    public const int PrefixLength = 191;
}
