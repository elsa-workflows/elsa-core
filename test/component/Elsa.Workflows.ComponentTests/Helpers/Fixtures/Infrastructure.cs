using Elsa.Expressions.Helpers;
using Testcontainers.MsSql;
using TUnit.Core.Interfaces;

namespace Elsa.Workflows.ComponentTests.Fixtures;

/// <summary>
/// Owns the SQL Server container shared by the native TUnit test session.
/// Individual test invocations create and drop their own catalogs through <see cref="App"/>.
/// </summary>
public sealed class Infrastructure : IAsyncInitializer, IAsyncDisposable
{
    private MsSqlContainer? _dbContainer;
    private bool _strictModeCaptured;
    private bool _originalStrictMode;

    public MsSqlContainer DbContainer => _dbContainer
        ?? throw new InvalidOperationException("The component-test SQL Server infrastructure has not been initialized.");

    public async Task InitializeAsync()
    {
        // Building the Testcontainers object performs Docker endpoint discovery and can throw.
        // Do that before mutating the process-wide parity setting.
        var dbContainer = new MsSqlBuilder().Build();
        _dbContainer = dbContainer;
        _originalStrictMode = ObjectConverter.StrictMode;
        _strictModeCaptured = true;
        ObjectConverter.StrictMode = true;

        try
        {
            await dbContainer.StartAsync();
        }
        catch (Exception initializationFailure)
        {
            Exception? cleanupFailure = null;
            try
            {
                await dbContainer.DisposeAsync();
            }
            catch (Exception exception)
            {
                cleanupFailure = exception;
            }
            finally
            {
                _dbContainer = null;
                RestoreStrictMode();
            }

            if (cleanupFailure is not null)
                throw new AggregateException("SQL Server infrastructure initialization and cleanup both failed.", initializationFailure, cleanupFailure);

            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Exception? failure = null;
        var dbContainer = _dbContainer;

        try
        {
            if (dbContainer is not null)
                await dbContainer.DisposeAsync();
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        finally
        {
            _dbContainer = null;
            RestoreStrictMode();
        }

        if (failure is not null)
            throw new AggregateException("Failed to dispose the component-test session infrastructure.", failure);
    }

    private void RestoreStrictMode()
    {
        if (!_strictModeCaptured)
            return;

        ObjectConverter.StrictMode = _originalStrictMode;
        _strictModeCaptured = false;
    }
}
