using Elsa.Studio.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using MudBlazor;
namespace Elsa.Studio.Workflows.Components
{
    /// <summary>
    /// Base component that provides common URL query parsing and update logic for
    /// table components that want to reflect state in the query string.
    /// Derived components must implement ApplyQueryParameters and BuildQueryFromState.
    /// </summary>
    public abstract class QueryTableComponentBase : StudioComponentBase
    {
        /// <summary>
        /// The injected navigation manager.
        /// </summary>
        [Inject] protected NavigationManager NavigationManager { get; set; } = null!;

        /// <summary>
        /// The injected logger.
        /// </summary>
        [Inject] protected ILogger<QueryTableComponentBase> Logger { get; set; } = null!;

        /// <summary>
        /// Specifies the initial page index used when starting pagination operations.
        /// </summary>
        protected int InitialPage { get; set; } = 0;

        /// <summary>
        /// Specifies the initial number of items to display per page.
        /// </summary>
        protected int InitialPageSize { get; set; } = 10;

        /// <summary>
        /// Indicates whether the component's state has been successfully initialized based
        /// on query parameters from the current URL. This property helps ensure query parsing
        /// happens only once unless explicitly forced.
        /// </summary>
        private bool InitializedFromQuery { get; set; }

        /// <inheritdoc/>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await ParseQueryParameters();
                StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        /// <summary>
        /// Parses the current URI's query string and delegates to <see cref="ApplyQueryParameters"/>.
        /// Only runs once unless you reset <see cref="InitializedFromQuery"/> or call ParseQueryParameters(force: true).
        /// </summary>
        private async Task ParseQueryParameters(bool force = false)
        {
            if (InitializedFromQuery && !force) return;

            try
            {
                var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
                var query = QueryHelpers.ParseQuery(uri.Query);

                // Convert StringValues to single string values
                var dict = query.ToDictionary(k => k.Key, kv => kv.Value.ToString(), StringComparer.OrdinalIgnoreCase);

                try
                {
                    await ApplyQueryParameters(dict);
                }
                catch (Exception ex)
                {
                    Logger?.LogDebug(ex, "Error while applying query parameters in derived component.");
                }

                InitializedFromQuery = true;
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Failed parsing query parameters.");
            }
        }

        /// <summary>
        /// Build and navigate to an updated URI that reflects the provided table state.
        /// Delegates construction of the query dictionary to <see cref="BuildQueryFromState"/>.
        /// </summary>
        protected void TryUpdateUrlFromState(TableState state)
        {
            try
            {
                if (!InitializedFromQuery)
                    return;

                var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
                var baseUri = uri.GetLeftPart(UriPartial.Path);
                var query = BuildQueryFromState(state);
                var dict = query.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
                var newUri = QueryHelpers.AddQueryString(baseUri, dict);

                if (!string.Equals(NavigationManager.Uri, newUri, StringComparison.Ordinal))
                    NavigationManager.NavigateTo(newUri, replace: true);
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Failed to update URL with table state.");
            }
        }

        /// <summary>
        /// Derived components must map parsed key/value pairs (from the query string)
        /// to their local component state in this method.
        /// </summary>
        /// <param name="query">Key -> string value dictionary (single values only).</param>
        //protected abstract void ApplyQueryParameters(IDictionary<string, string> query);
        protected abstract Task ApplyQueryParameters(IDictionary<string, string> query);

        /// <summary>
        /// Derived components must return the query dictionary to represent the current state
        /// (keys -> values). Values that are null or empty will be excluded before navigation.
        /// </summary>
        protected abstract Dictionary<string, string?> BuildQueryFromState(TableState state);

        /// <summary>
        /// Convenience helper used by components that need the timestamp filter encoding.
        /// </summary>
        protected static string EncodeTimestampFiltersToBase64Json<T>(IEnumerable<T> filters)
        {
            var opts = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { WriteIndented = false };
            var json = System.Text.Json.JsonSerializer.Serialize(filters, opts);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }
    }
}