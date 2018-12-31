using System;
using System.Threading.Tasks;
using Elsa.Web.ViewModels;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;

namespace Elsa.Web.Drivers
{
    /// <summary>
    /// Base class for activity drivers.
    /// </summary>
    public abstract class ActivityDisplayDriver<TActivity> : DisplayDriver<IActivity, TActivity> where TActivity : class, IActivity
    {
        public override IDisplayResult Display(TActivity model)
        {
            return Combine(
                Shape(GetDesignShapeType(model), new ActivityShapeModel<TActivity>(model)).Location("Design", "Content"),
                Shape(GetCardShapeType(model), new ActivityShapeModel<TActivity>(model)).Location("Card", "Content")
            );
        }

        protected string GetDesignShapeType(IActivity activity) => $"{activity.Name}_Design";
        protected string GetCardShapeType(IActivity activity) => $"{activity.Name}_Card";
    }

    /// <summary>
    /// Base class for activity drivers using a strongly typed view model.
    /// </summary>
    public abstract class ActivityDisplayDriver<TActivity, TEditViewModel> : ActivityDisplayDriver<TActivity>
        where TActivity : class, IActivity
        where TEditViewModel : class, new()
    {
        public override IDisplayResult Edit(TActivity model)
        {
            var result = Initialize(
                    GetEditorShapeType(model),
                    (Func<TEditViewModel, Task>) (viewModel => EditActivityAsync(model, viewModel)))
                .Location("Content");

            return result;
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

        protected virtual string GetEditorShapeType(TActivity activity) => $"{activity.Name}_Edit";
    }
}