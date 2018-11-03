using System;
using System.Threading.Tasks;
using Flowsharp.Web.Abstractions.ViewModels;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;

namespace Flowsharp.Web.Abstractions.Drivers
{
    /// <summary>
    /// Base class for activity drivers.
    /// </summary>
    public abstract class ActivityDisplayDriver<TActivity> : DisplayDriver<IActivity, TActivity> where TActivity : class, IActivity
    {
        private static readonly string ThumbnailShapeType = $"{typeof(TActivity).Name}_Thumbnail";
        private static readonly string DesignShapeType = $"{typeof(TActivity).Name}_Design";

        public override IDisplayResult Display(TActivity model)
        {
            return Combine(
                Shape(ThumbnailShapeType, new ActivityViewModel<TActivity>(model)).Location("Thumbnail", "Content"),
                Shape(DesignShapeType, new ActivityViewModel<TActivity>(model)).Location("Design", "Content")
            );
        }
    }

    /// <summary>
    /// Base class for activity drivers using a strongly typed view model.
    /// </summary>
    public abstract class ActivityDisplayDriver<TActivity, TEditViewModel> : ActivityDisplayDriver<TActivity> where TActivity : class, IActivity where TEditViewModel : class, new()
    {
        private static readonly string EditShapeType = $"{typeof(TActivity).Name}_Fields_Edit";

        public override IDisplayResult Edit(TActivity model)
        {
            return Initialize(EditShapeType, (Func<TEditViewModel, Task>)(viewModel => EditActivityAsync(model, viewModel))).Location("Content");
        }

        public override async Task<IDisplayResult> UpdateAsync(TActivity model, IUpdateModel updater)
        {
            var viewModel = new TEditViewModel();

            if (await updater.TryUpdateModelAsync(viewModel, Prefix))
                await UpdateActivityAsync(viewModel, model);

            return Edit(model);
        }

        /// <summary>
        /// Edit the view model before it's used in the editor.
        /// </summary>
        protected virtual Task EditActivityAsync(TActivity activity, TEditViewModel model)
        {
            EditActivity(activity, model);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Edit the view model before it's used in the editor.
        /// </summary>
        protected virtual void EditActivity(TActivity activity, TEditViewModel model)
        {
        }

        /// <summary>
        /// Updates the activity when the view model is validated.
        /// </summary>
        protected virtual Task UpdateActivityAsync(TEditViewModel model, TActivity activity)
        {
            UpdateActivity(model, activity);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates the activity when the view model is validated.
        /// </summary>
        protected virtual void UpdateActivity(TEditViewModel model, TActivity activity)
        {
        }
    }
}
