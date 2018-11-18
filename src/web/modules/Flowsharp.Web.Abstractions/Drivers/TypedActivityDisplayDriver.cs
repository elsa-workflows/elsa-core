using Flowsharp.Models;

namespace Flowsharp.Web.Abstractions.Drivers
{
    /// <summary>
    /// Base class for activity drivers.
    /// </summary>
    public abstract class TypedActivityDisplayDriver<TActivity> : ActivityDisplayDriver where TActivity : class, IActivity
    {
        public override bool CanHandleModel(IActivity model) => model.Name == typeof(TActivity).Name;
    }
    
    /// <summary>
    /// Base class for activity drivers using a strongly typed view model.
    /// </summary>
    public abstract class TypedActivityDisplayDriver<TActivity, TEditViewModel> : ActivityDisplayDriver<TEditViewModel> where TActivity : class, IActivity where TEditViewModel : class, new()
    {
        public override bool CanHandleModel(IActivity model) => model.Name == typeof(TActivity).Name;
    }
}