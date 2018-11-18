using System;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Web.Abstractions.ViewModels;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;

namespace Flowsharp.Web.Abstractions.Drivers
{
    /// <summary>
    /// Base class for activity drivers.
    /// </summary>
    public abstract class ActivityDisplayDriver : DisplayDriver<IActivity>
    {
        public override IDisplayResult Display(IActivity model)
        {
            return Shape(GetDesignShapeType(model), new ActivityViewModel(model)).Location("Design", "Content");
        }

        protected string GetDesignShapeType(IActivity activity) => $"{activity.Name}_Design";
    }
    
    /// <summary>
    /// Base class for activity drivers using a strongly typed view model.
    /// </summary>
    public abstract class ActivityDisplayDriver<TEditViewModel> : ActivityDisplayDriver where TEditViewModel : class, new()
    {
        public override IDisplayResult Edit(IActivity model)
        {
            return Initialize(GetEditorShapeType(model), (Func<TEditViewModel, Task>)(viewModel => EditActivityAsync(model, viewModel))).Location("Content");
        }

        public override async Task<IDisplayResult> UpdateAsync(IActivity model, IUpdateModel updater)
        {
            var viewModel = new TEditViewModel();

            if (await updater.TryUpdateModelAsync(viewModel, Prefix))
                await UpdateActivityAsync(viewModel, model);

            return Edit(model);
        }

        /// <summary>
        /// Edit the view model before it's used in the editor.
        /// </summary>
        protected virtual Task EditActivityAsync(IActivity activity, TEditViewModel model)
        {
            EditActivity(activity, model);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Edit the view model before it's used in the editor.
        /// </summary>
        protected virtual void EditActivity(IActivity activity, TEditViewModel model)
        {
        }

        /// <summary>
        /// Updates the activity when the view model is validated.
        /// </summary>
        protected virtual Task UpdateActivityAsync(TEditViewModel model, IActivity activity)
        {
            UpdateActivity(model, activity);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates the activity when the view model is validated.
        /// </summary>
        protected virtual void UpdateActivity(TEditViewModel model, IActivity activity)
        {
        }
        
        protected string GetEditorShapeType(IActivity activity) => $"{activity.Name}_Edit";
    }
}