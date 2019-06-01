using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement;

namespace Elsa.Web.Components.ViewModels
{
    public class ActivityPickerViewModel
    {
        public ActivityPickerViewModel(
            IEnumerable<LocalizedString> categories, 
            IEnumerable<ActivityDesignerDescriptor> activityDescriptors, 
            IEnumerable<IShape> cardShapes)
        {
            ActivityCategories = categories.ToList();
            ActivityDescriptors = activityDescriptors.ToList();
            ActivityCardShapes = cardShapes.ToList();
        }

        public IReadOnlyCollection<LocalizedString> ActivityCategories { get; }
        public IReadOnlyCollection<ActivityDesignerDescriptor> ActivityDescriptors { get; }
        public IReadOnlyCollection<IShape> ActivityCardShapes { get; }
    }
}