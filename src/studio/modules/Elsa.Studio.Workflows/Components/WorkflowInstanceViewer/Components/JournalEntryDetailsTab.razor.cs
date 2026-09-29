using System.Text.Json;
using Elsa.Studio.Components;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// Displays the details of a journal entry.
public partial class JournalEntryDetailsTab
{
    /// The journal entry.
    [Parameter] public JournalEntry JournalEntry { get; set; } = default!;

    /// The height of the visible pane.
    [Parameter] public int VisiblePaneHeight { get; set; }
    
    [Inject] private ITimeFormatter TimeFormatter { get; set; } = default!;

    private DataPanelModel ParsePayload(object? payload)
    {
        if (payload == null)
            return new();

        if (payload is not JsonElement jsonElement)
            return [new DataPanelItem("Payload", payload.ToString())];

        var properties = jsonElement.EnumerateObject().Where(x => !x.Name.StartsWith("_"));
        var result = new DataPanelModel();

        foreach (var property in properties)
        {
            var propertyName = property.Name.Pascalize();
            var propertyValue = property.Value;
            var propertyValueAsString = propertyValue.ToString();
            result.Add(propertyName, propertyValueAsString);
        }

        return result;
    }

    private static void Merge(DataPanelModel target, DataPanelModel input)
    {
        foreach (var item in input)
        {
            var existingItem = target.FirstOrDefault(x => x.Label == item.Label);
            
            if (existingItem != null) 
                target.Remove(existingItem);
            
            target.Add(item);
        }
    }
}