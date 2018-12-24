using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement;

namespace Flowsharp.Web.ViewComponents.ViewModels
{
    public class ActivityPickerViewModel
    {
        public ActivityPickerViewModel(IEnumerable<IShape> activityShapes, IEnumerable<LocalizedString> categories)
        {
            ActivityCategories = categories.ToList();
            ActivityShapes = activityShapes.ToList();
        }

        public IReadOnlyCollection<LocalizedString> ActivityCategories { get; set; }
        public IReadOnlyCollection<IShape> ActivityShapes { get; }
    }
}