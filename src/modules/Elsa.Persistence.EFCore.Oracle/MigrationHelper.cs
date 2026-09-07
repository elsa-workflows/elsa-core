using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EFCore.Oracle;

/// <summary>
/// Emits Oracle-specific migration statements that work around DDL restrictions Oracle imposes and that survive being
/// re-run after a partially applied attempt.
/// </summary>
/// <remarks>
/// Oracle commits implicitly before and after every DDL statement, so a migration that fails halfway leaves the
/// statements it already executed committed - EF Core's migration transaction cannot roll them back. Every operation
/// emitted by this helper therefore first inspects <c>ALL_TAB_COLUMNS</c>, or traps the Oracle error code that means
/// "this was already done", and skips the work it finds already applied. Re-running a failed migration then converges
/// on the intended schema instead of failing with, for example, ORA-01430 ("column being added already exists").
/// </remarks>
internal static class MigrationHelper
{
    /// <summary>
    /// The suffix of the temporary column <see cref="ConvertColumnType"/> builds the converted values in. The same
    /// suffix is used in both directions on purpose: a temporary column left behind by a failed run is then always
    /// noticed by the next run instead of being orphaned under a direction-specific name.
    /// </summary>
    private const string TempColumnSuffix = "_New";

    /// <summary>
    /// Changes the datatype of <paramref name="column"/> by adding a temporary column of the target type, copying the
    /// values across, dropping the original column and renaming the temporary one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Oracle refuses to change a column's datatype to or from a LOB type in place: <c>ALTER TABLE ... MODIFY</c>
    /// fails with ORA-22858 ("invalid alteration of datatype") when the target type is a LOB, and with ORA-22859
    /// ("invalid modification of columns") when the source type is. The add/copy/drop/rename sequence emitted here is
    /// the workaround the ORA-22858 message itself prescribes.
    /// </para>
    /// <para>
    /// The emitted PL/SQL block is re-runnable. It derives what still needs doing from the datatypes currently
    /// recorded in <c>ALL_TAB_COLUMNS</c> for the original and the temporary column, so it converges from every state
    /// a previously failed run can leave behind: nothing done yet, the temporary column added, the values copied, or
    /// the original column already dropped. When the conversion has already completed it does nothing at all, which is
    /// what lets an operator who applied the conversion by hand replay the migration. When it finds a datatype it does
    /// not recognize it raises, rather than risk converting an already-converted column and losing the data.
    /// </para>
    /// </remarks>
    /// <param name="migrationBuilder">The migration builder to emit into.</param>
    /// <param name="schema">The Elsa schema the table lives in.</param>
    /// <param name="table">The unquoted table name.</param>
    /// <param name="column">The unquoted column name.</param>
    /// <param name="fromDataType">The current datatype, as <c>ALL_TAB_COLUMNS.DATA_TYPE</c> reports it (for example <c>NCLOB</c> or <c>NVARCHAR2</c>).</param>
    /// <param name="toDataType">The target datatype, as <c>ALL_TAB_COLUMNS.DATA_TYPE</c> reports it. This is what marks the conversion as already done, so it has to be the dictionary spelling and not the DDL spelling.</param>
    /// <param name="toColumnDefinition">The target datatype as written in DDL, including any length (for example <c>NVARCHAR2(450)</c>).</param>
    /// <param name="copyExpression">Builds the SQL expression that converts a value; it is handed the quoted name of the original column.</param>
    /// <param name="notNull">Whether the converted column must carry a NOT NULL constraint. The constraint is applied by a separate, equally re-runnable statement once the conversion has landed.</param>
    public static void ConvertColumnType(
        MigrationBuilder migrationBuilder,
        IElsaDbContextSchema schema,
        string table,
        string column,
        string fromDataType,
        string toDataType,
        string toColumnDefinition,
        Func<string, string> copyExpression,
        bool notNull)
    {
        var qualifiedTable = QualifyTable(schema, table);
        var tempColumn = $"{column}{TempColumnSuffix}";
        var copy = copyExpression($"\"{column}\"");

        migrationBuilder.Sql($"""
                              DECLARE
                                  l_source_type ALL_TAB_COLUMNS.DATA_TYPE%TYPE;
                                  l_temp_type ALL_TAB_COLUMNS.DATA_TYPE%TYPE;
                                  l_already_converted BOOLEAN := FALSE;

                                  FUNCTION data_type_of(p_column IN VARCHAR2) RETURN VARCHAR2 IS
                                      l_data_type ALL_TAB_COLUMNS.DATA_TYPE%TYPE;
                                  BEGIN
                                      SELECT DATA_TYPE INTO l_data_type FROM ALL_TAB_COLUMNS WHERE OWNER = '{schema.Schema}' AND TABLE_NAME = '{table}' AND COLUMN_NAME = p_column;
                                      RETURN l_data_type;
                                  EXCEPTION
                                      WHEN NO_DATA_FOUND THEN
                                          RETURN NULL;
                                  END;
                              BEGIN
                                  l_source_type := data_type_of('{column}');
                                  l_temp_type := data_type_of('{tempColumn}');

                                  -- The conversion already completed, either on an earlier run of this migration or by hand.
                                  IF l_temp_type IS NULL AND l_source_type = '{toDataType}' THEN
                                      l_already_converted := TRUE;
                                  END IF;

                                  IF NOT l_already_converted THEN
                                      IF l_source_type IS NULL AND l_temp_type IS NULL THEN
                                          RAISE_APPLICATION_ERROR(-20001, 'Neither "{column}" nor "{tempColumn}" exists on {qualifiedTable}.');
                                      END IF;

                                      -- Refuse to convert a column whose datatype is not one this migration knows about: converting
                                      -- blindly would copy out of, and then drop, a column that may already hold the converted data.
                                      IF l_source_type IS NOT NULL AND l_source_type != '{fromDataType}' THEN
                                          RAISE_APPLICATION_ERROR(-20002, 'Expected "{column}" on {qualifiedTable} to be {fromDataType} or {toDataType}, but found ' || l_source_type || '.');
                                      END IF;

                                      IF l_temp_type IS NOT NULL AND l_temp_type != '{toDataType}' THEN
                                          RAISE_APPLICATION_ERROR(-20003, 'Expected the leftover column "{tempColumn}" on {qualifiedTable} to be {toDataType}, but found ' || l_temp_type || '.');
                                      END IF;

                                      IF l_temp_type IS NULL THEN
                                          EXECUTE IMMEDIATE 'ALTER TABLE {qualifiedTable} ADD ("{tempColumn}" {toColumnDefinition})';
                                      END IF;

                                      IF l_source_type IS NOT NULL THEN
                                          EXECUTE IMMEDIATE 'UPDATE {qualifiedTable} SET "{tempColumn}" = {copy} WHERE "{column}" IS NOT NULL';
                                          EXECUTE IMMEDIATE 'ALTER TABLE {qualifiedTable} DROP COLUMN "{column}"';
                                      END IF;

                                      EXECUTE IMMEDIATE 'ALTER TABLE {qualifiedTable} RENAME COLUMN "{tempColumn}" TO "{column}"';
                                  END IF;
                              END;
                              """);

        if (notNull)
        {
            SetColumnNotNull(migrationBuilder, schema, table, column);
        }
    }

    /// <summary>
    /// Raises an error if any value stored in <paramref name="column"/> is longer than <paramref name="maxLength"/>
    /// characters, so that <see cref="ConvertColumnType"/> converting a LOB column down to a bounded
    /// <c>NVARCHAR2</c> fails before it copies - and so silently truncates - a value that does not fit.
    /// </summary>
    /// <remarks>
    /// The check only runs while <paramref name="column"/> is still recorded as <c>NCLOB</c> or <c>CLOB</c> in
    /// <c>ALL_TAB_COLUMNS</c>: once the conversion has landed the datatype itself already enforces the length, and a
    /// static reference to <c>DBMS_LOB.GETLENGTH</c> against a non-LOB column would fail to compile even inside a
    /// branch that never runs, so the length query itself is dynamic SQL to defer that type check to when the guard
    /// actually applies. That also makes a re-run after the conversion has completed a no-op, consistent with every
    /// other helper in this class.
    /// </remarks>
    /// <param name="migrationBuilder">The migration builder to emit into.</param>
    /// <param name="schema">The Elsa schema the table lives in.</param>
    /// <param name="table">The unquoted table name.</param>
    /// <param name="column">The unquoted column name.</param>
    /// <param name="maxLength">The maximum number of characters the target <c>NVARCHAR2</c> column can hold.</param>
    public static void EnsureLobLengthAtMost(MigrationBuilder migrationBuilder, IElsaDbContextSchema schema, string table, string column, int maxLength)
    {
        var qualifiedTable = QualifyTable(schema, table);

        migrationBuilder.Sql($"""
                              DECLARE
                                  l_data_type ALL_TAB_COLUMNS.DATA_TYPE%TYPE;
                                  l_count INTEGER;
                              BEGIN
                                  SELECT DATA_TYPE INTO l_data_type FROM ALL_TAB_COLUMNS WHERE OWNER = '{schema.Schema}' AND TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}';

                                  IF l_data_type IN ('NCLOB', 'CLOB') THEN
                                      EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM {qualifiedTable} WHERE DBMS_LOB.GETLENGTH("{column}") > {maxLength}' INTO l_count;

                                      IF l_count > 0 THEN
                                          RAISE_APPLICATION_ERROR(-20004, l_count || ' row(s) in {qualifiedTable} have "{column}" values longer than {maxLength} characters; they cannot be stored in NVARCHAR2({maxLength}). Shorten or remove them before downgrading.');
                                      END IF;
                                  END IF;
                              EXCEPTION
                                  WHEN NO_DATA_FOUND THEN
                                      NULL;
                              END;
                              """);
    }

    /// <summary>
    /// Adds a column unless it is already there, so that a run following a partially applied one does not fail with
    /// ORA-01430 ("column being added already exists in table").
    /// </summary>
    /// <param name="migrationBuilder">The migration builder to emit into.</param>
    /// <param name="schema">The Elsa schema the table lives in.</param>
    /// <param name="table">The unquoted table name.</param>
    /// <param name="column">The unquoted column name.</param>
    /// <param name="columnDefinition">The column definition as written in DDL, for example <c>NCLOB</c>.</param>
    public static void AddColumnIfMissing(MigrationBuilder migrationBuilder, IElsaDbContextSchema schema, string table, string column, string columnDefinition)
    {
        var qualifiedTable = QualifyTable(schema, table);

        migrationBuilder.Sql($"""
                              DECLARE
                                  l_count INTEGER;
                              BEGIN
                                  SELECT COUNT(*) INTO l_count FROM ALL_TAB_COLUMNS WHERE OWNER = '{schema.Schema}' AND TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}';

                                  IF l_count = 0 THEN
                                      EXECUTE IMMEDIATE 'ALTER TABLE {qualifiedTable} ADD ("{column}" {columnDefinition})';
                                  END IF;
                              END;
                              """);
    }

    /// <summary>
    /// Drops a column if it is there, so that a run following a partially applied one does not fail with ORA-00904
    /// ("invalid identifier").
    /// </summary>
    /// <param name="migrationBuilder">The migration builder to emit into.</param>
    /// <param name="schema">The Elsa schema the table lives in.</param>
    /// <param name="table">The unquoted table name.</param>
    /// <param name="column">The unquoted column name.</param>
    public static void DropColumnIfPresent(MigrationBuilder migrationBuilder, IElsaDbContextSchema schema, string table, string column)
    {
        var qualifiedTable = QualifyTable(schema, table);

        migrationBuilder.Sql($"""
                              DECLARE
                                  l_count INTEGER;
                              BEGIN
                                  SELECT COUNT(*) INTO l_count FROM ALL_TAB_COLUMNS WHERE OWNER = '{schema.Schema}' AND TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}';

                                  IF l_count > 0 THEN
                                      EXECUTE IMMEDIATE 'ALTER TABLE {qualifiedTable} DROP COLUMN "{column}"';
                                  END IF;
                              END;
                              """);
    }

    /// <summary>
    /// Creates an index unless it is already there, so that a run following a partially applied one does not fail with
    /// ORA-00955 ("name is already used by an existing object"). When an index with that name already exists, its
    /// table, uniqueness and ordered column list are validated against what is being requested, so that an index
    /// schema drift or manual recovery left behind under the same name is not mistaken for the one this migration
    /// means to create.
    /// </summary>
    /// <param name="migrationBuilder">The migration builder to emit into.</param>
    /// <param name="schema">The Elsa schema the table lives in.</param>
    /// <param name="name">The unquoted index name.</param>
    /// <param name="table">The unquoted table name.</param>
    /// <param name="columns">The unquoted column names to index.</param>
    /// <param name="unique">Whether the index is unique.</param>
    public static void CreateIndexIfMissing(MigrationBuilder migrationBuilder, IElsaDbContextSchema schema, string name, string table, string[] columns, bool unique = false)
    {
        var columnList = string.Join(", ", columns.Select(x => $"\"{x}\""));
        var createIndex = $"CREATE {(unique ? "UNIQUE " : "")}INDEX \"{schema.Schema}\".\"{name}\" ON {QualifyTable(schema, table)} ({columnList})";
        var expectedUniqueness = unique ? "UNIQUE" : "NONUNIQUE";
        var expectedColumns = string.Join(",", columns);

        migrationBuilder.Sql($"""
                              DECLARE
                                  l_count INTEGER;
                                  l_table_name ALL_INDEXES.TABLE_NAME%TYPE;
                                  l_uniqueness ALL_INDEXES.UNIQUENESS%TYPE;
                                  l_columns VARCHAR2(4000);
                              BEGIN
                                  SELECT COUNT(*) INTO l_count FROM ALL_INDEXES WHERE OWNER = '{schema.Schema}' AND INDEX_NAME = '{name}';

                                  IF l_count = 0 THEN
                                      EXECUTE IMMEDIATE '{createIndex.Replace("'", "''")}';
                                  ELSE
                                      SELECT TABLE_NAME, UNIQUENESS INTO l_table_name, l_uniqueness FROM ALL_INDEXES WHERE OWNER = '{schema.Schema}' AND INDEX_NAME = '{name}';
                                      SELECT LISTAGG(COLUMN_NAME, ',') WITHIN GROUP (ORDER BY COLUMN_POSITION) INTO l_columns FROM ALL_IND_COLUMNS WHERE INDEX_OWNER = '{schema.Schema}' AND INDEX_NAME = '{name}';

                                      IF l_table_name != '{table}' OR l_uniqueness != '{expectedUniqueness}' OR l_columns != '{expectedColumns}' THEN
                                          RAISE_APPLICATION_ERROR(-20005, 'Index "{schema.Schema}"."{name}" already exists but does not match the expected definition (table {table}, {expectedUniqueness}, columns {expectedColumns}); found table ' || l_table_name || ', ' || l_uniqueness || ', columns ' || l_columns || '. Drop or fix the existing index before running this migration.');
                                      END IF;
                                  END IF;
                              END;
                              """);
    }

    /// <summary>
    /// Drops an index if it is there, so that a run following a partially applied one does not fail with ORA-01418
    /// ("specified index does not exist").
    /// </summary>
    /// <param name="migrationBuilder">The migration builder to emit into.</param>
    /// <param name="schema">The Elsa schema the index lives in.</param>
    /// <param name="name">The unquoted index name.</param>
    public static void DropIndexIfPresent(MigrationBuilder migrationBuilder, IElsaDbContextSchema schema, string name)
    {
        SqlIgnoringOracleError(migrationBuilder, $"DROP INDEX \"{schema.Schema}\".\"{name}\"", -1418, "ORA-01418: the index is already gone, so an earlier run got at least this far.");
    }

    private static string QualifyTable(IElsaDbContextSchema schema, string table) => $"\"{schema.Schema}\".\"{table}\"";

    /// <summary>
    /// Constrains a column to NOT NULL unless it already is, so that re-running does not fail with ORA-01442 ("column
    /// to be modified to NOT NULL is already NOT NULL"). Adding the constraint is not a datatype alteration, so unlike
    /// the conversion itself Oracle does allow it in place - including on a LOB column.
    /// </summary>
    private static void SetColumnNotNull(MigrationBuilder migrationBuilder, IElsaDbContextSchema schema, string table, string column)
    {
        var qualifiedTable = QualifyTable(schema, table);

        migrationBuilder.Sql($"""
                              DECLARE
                                  l_nullable ALL_TAB_COLUMNS.NULLABLE%TYPE;
                              BEGIN
                                  SELECT NULLABLE INTO l_nullable FROM ALL_TAB_COLUMNS WHERE OWNER = '{schema.Schema}' AND TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}';

                                  IF l_nullable = 'Y' THEN
                                      EXECUTE IMMEDIATE 'ALTER TABLE {qualifiedTable} MODIFY ("{column}" NOT NULL)';
                                  END IF;
                              END;
                              """);
    }

    private static void SqlIgnoringOracleError(MigrationBuilder migrationBuilder, string sql, int sqlCode, string comment)
    {
        var literal = sql.Replace("'", "''");

        migrationBuilder.Sql($"""
                              BEGIN
                                  EXECUTE IMMEDIATE '{literal}';
                              EXCEPTION
                                  WHEN OTHERS THEN
                                      -- {comment}
                                      IF SQLCODE != {sqlCode} THEN RAISE; END IF;
                              END;
                              """);
    }
}
