using Elsa.Studio.DomInterop.Contracts;
using Microsoft.JSInterop;

namespace Elsa.Studio.DomInterop.Interop;

/// <summary>
/// Provides JS interop methods for the files module.
/// </summary>
public class FilesJsInterop : JsInteropBase, IFiles
{
    /// <inheritdoc />
    public FilesJsInterop(IJSRuntime jsRuntime) : base(jsRuntime)
    {
    }

    /// <inheritdoc />
    protected override string ModuleName => "files";

    /// <inheritdoc />
    public async Task DownloadFileFromStreamAsync(string fileName, Stream stream)
    {
        var streamRef = new DotNetStreamReference(stream);
        await InvokeAsync(async module => await module.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef));
    }
}