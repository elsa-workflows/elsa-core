using Elsa.Studio.Contracts;
using MudBlazor;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultUserMessageService : IUserMessageService
{
    private readonly ISnackbar _snackbar;

    /// <summary> Constructor. </summary>
    public DefaultUserMessageService(ISnackbar snackbar)
    {
        _snackbar = snackbar;
    }

    /// <inheritdoc />
    public void ShowSnackbarTextMessage(string message, Severity severity = Severity.Normal, Action<SnackbarOptions>? snackbarOptions = null)
    {
        _snackbar.Add(message, severity, snackbarOptions);
    }

    /// <inheritdoc />
    public void ShowSnackbarTextMessage(IEnumerable<string> messages, Severity severity = Severity.Normal, Action<SnackbarOptions>? snackbarOptions = null)
    {
        foreach (var message in messages)
        {
            _snackbar.Add(message, severity, snackbarOptions);
        }
    }
}