using System.Collections.Generic;
using OrchardCore.DisplayManagement;

namespace Flowsharp.Web.ViewComponents.ViewModels
{
    public class ActivityLibraryViewModel
    {
        public ActivityLibraryViewModel(IEnumerable<IShape> activityShapes)
        {
            ActivityShapes = activityShapes;
        }

        public IEnumerable<IShape> ActivityShapes { get; }
    }
}