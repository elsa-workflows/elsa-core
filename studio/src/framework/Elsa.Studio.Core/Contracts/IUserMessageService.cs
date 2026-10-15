using MudBlazor;

namespace Elsa.Studio.Contracts;

/// <summary> Represents a service for displaying user messages. </summary>
public interface IUserMessageService
{
    /// <summary> Displays a message using snackbar component. </summary>
    void ShowSnackbarTextMessage(string message, Severity severity = Severity.Normal, Action<SnackbarOptions>? snackbarOptions = null);

    /// <summary> Displays messages using snackbar component. </summary>
    void ShowSnackbarTextMessage(IEnumerable<string> messages, Severity severity = Severity.Normal, Action<SnackbarOptions>? snackbarOptions = null);
}