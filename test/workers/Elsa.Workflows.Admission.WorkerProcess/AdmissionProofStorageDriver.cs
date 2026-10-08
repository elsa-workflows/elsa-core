using Npgsql;

namespace Elsa.Workflows.Admission.WorkerProcess;

/// <summary>A real, isolated PostgreSQL fixture driver for discriminating variable load/save proof.</summary>
public sealed class AdmissionProofStorageDriver(string connectionString, AdmissionRuntimeProbe probe) : IStorageDriver
{
    public double Priority => 0;
    public IEnumerable<string> Tags => [];

    public async ValueTask WriteAsync(string id, object value, StorageDriverContext context)
    {
        if (value is not string text)
        {
            throw new InvalidOperationException("fixture_variable_value_unsupported");
        }
        await ReplacePersistedAsync(InstanceId(context), id, text, context.CancellationToken);
        await probe.IncrementAsync("variableWrites");
    }

    public async ValueTask<object?> ReadAsync(string id, StorageDriverContext context)
    {
        var value = await ReadPersistedAsync(InstanceId(context), id, context.CancellationToken);
        await probe.IncrementAsync("variableReads");
        return value;
    }

    public async ValueTask DeleteAsync(string id, StorageDriverContext context)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(context.CancellationToken);
        await using var command = new NpgsqlCommand("DELETE FROM \"AdmissionRuntimeProofVariables\" WHERE \"InstanceId\" = @instance AND \"VariableId\" = @variable", connection);
        Bind(command, InstanceId(context), id);
        await command.ExecuteNonQueryAsync(context.CancellationToken);
        await probe.IncrementAsync("variableDeletes");
    }

    public async Task<string?> ReadPersistedAsync(string instanceId, string variableId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT \"Value\" FROM \"AdmissionRuntimeProofVariables\" WHERE \"InstanceId\" = @instance AND \"VariableId\" = @variable", connection);
        Bind(command, instanceId, variableId);
        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }

    public async Task ReplacePersistedAsync(string instanceId, string variableId, string value, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("INSERT INTO \"AdmissionRuntimeProofVariables\" (\"InstanceId\", \"VariableId\", \"Value\") VALUES (@instance, @variable, @value) ON CONFLICT (\"InstanceId\", \"VariableId\") DO UPDATE SET \"Value\" = EXCLUDED.\"Value\"", connection);
        Bind(command, instanceId, variableId);
        command.Parameters.AddWithValue("value", value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string InstanceId(StorageDriverContext context) =>
        ((ActivityExecutionContext)context.ExecutionContext).WorkflowExecutionContext.Id;

    private static void Bind(NpgsqlCommand command, string instanceId, string variableId)
    {
        command.Parameters.AddWithValue("instance", instanceId);
        command.Parameters.AddWithValue("variable", variableId);
    }
}
