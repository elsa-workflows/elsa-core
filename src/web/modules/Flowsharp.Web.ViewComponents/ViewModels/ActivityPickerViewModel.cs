using System.Collections.Generic;
using System.Linq;
using Flowsharp.Models;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement;

namespace Flowsharp.Web.ViewComponents.ViewModels
{
    public class ActivityPickerViewModel
    {
        public ActivityPickerViewModel(
            IEnumerable<LocalizedString> categories, 
            IEnumerable<ActivityDescriptor> activityDescriptors, 
            IEnumerable<IShape> cardShapes)
        {
            ActivityCategories = categories.ToList();
            ActivityDescriptors = activityDescriptors.ToList();
            ActivityCardShapes = cardShapes.ToList();
        }

        public IReadOnlyCollection<LocalizedString> ActivityCategories { get; }
        public IReadOnlyCollection<ActivityDescriptor> ActivityDescriptors { get; }
        public IReadOnlyCollection<IShape> ActivityCardShapes { get; }
    }
}