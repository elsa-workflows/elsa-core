import {Component, Host, h, State} from '@stencil/core';
import '../../../../utils/utils';
import {eventBus} from '../../../../utils/event-bus';
import {EventTypes} from "../../../../models/events";
import {ActivityDescriptor, ActivityModel, ActivityPropertyDescriptor} from "../../../../models/domain";
import state from '../../../../utils/store';

@Component({
  tag: 'elsa-activity-editor-modal',
  styleUrl: 'elsa-activity-editor-modal.css',
  shadow: false,
})
export class ElsaActivityPickerModal {

  @State() activityModel: ActivityModel;
  activityDescriptor: ActivityDescriptor;
  dialog: HTMLElsaModalDialogElement;

  componentDidLoad() {
    eventBus.on(EventTypes.ShowActivityEditor, async (activity: ActivityModel) => {
      this.activityModel = {...activity};
      this.activityDescriptor = state.activityDescriptors.find(x => x.type == activity.type);
      await this.dialog.show();
    });
  }

  async onCancelClick() {
    await this.dialog.hide();
  }

  async onSubmit(e: Event){
    e.preventDefault();
    await this.dialog.hide();
  }

  render() {
    const activityModel = this.activityModel || {type: '', activityId: '', outcomes: []};
    const activityDescriptor = this.activityDescriptor || {displayName: '', type: '', outcomes: [], category: '', traits: 0, browsable: false, properties: [], description: ''};
    const propertyDescriptors: Array<ActivityPropertyDescriptor> = activityDescriptor.properties;

    return (
      <Host>
        <elsa-modal-dialog ref={el => this.dialog = el}>
          <div slot="content" class="py-8 pb-0">
            <form onSubmit={e => this.onSubmit(e)}>
              <div class="flex px-8">
                <div class="space-y-8 divide-y divide-gray-200 w-full">
                  <div>
                    <div>
                      <h3 class="text-lg leading-6 font-medium text-gray-900">
                        {activityDescriptor.displayName}
                      </h3>
                      <p class="mt-1 text-sm text-gray-500">
                        {activityDescriptor.description}
                      </p>
                    </div>

                    <div class="mt-6 grid grid-cols-1 gap-y-6 gap-x-4 sm:grid-cols-6">

                      {propertyDescriptors.map(x => this.renderPropertyEditor(x, activityModel))}

                      <div class="sm:col-span-6">
                        <label htmlFor="username" class="block text-sm font-medium text-gray-700">
                          Username
                        </label>
                        <div class="mt-1">
                          <input type="text" name="username" id="username" autocomplete="username" class="focus:ring-blue-500 focus:border-blue-500 block w-full min-w-0 rounded-md sm:text-sm border-gray-300"/>
                        </div>
                      </div>

                      <div class="sm:col-span-6">
                        <label htmlFor="about" class="block text-sm font-medium text-gray-700">
                          About
                        </label>
                        <div class="mt-1">
                          <textarea id="about" name="about" rows={3} class="shadow-sm focus:ring-blue-500 focus:border-blue-500 block w-full sm:text-sm border-gray-300 rounded-md"/>
                        </div>
                        <p class="mt-2 text-sm text-gray-500">Write a few sentences about yourself.</p>
                      </div>
                    </div>
                  </div>

                </div>
              </div>
              <div class="pt-5">
                <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
                  <button type="submit" class="ml-3 inline-flex justify-center py-2 px-4 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
                    Save
                  </button>
                  <button type="button"
                          onClick={() => this.onCancelClick()}
                          class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm">
                    Cancel
                  </button>
                </div>
              </div>
            </form>
          </div>

          <div slot="buttons"/>
        </elsa-modal-dialog>
      </Host>
    );
  }

  renderPropertyEditor(propertyDescriptor: ActivityPropertyDescriptor, activity: ActivityModel){
    const key = `${activity.activityId}:${propertyDescriptor.name}`;
    const fieldId = propertyDescriptor.name;
    const fieldLabel = propertyDescriptor.label || propertyDescriptor.name;
    const fieldHint = propertyDescriptor.hint;

    return (
      <div key={key} class="sm:col-span-6">
        <label htmlFor={fieldId} class="block text-sm font-medium text-gray-700">
          {fieldLabel}
        </label>
        <div class="mt-1">
          <input type="text" id={fieldId} class="focus:ring-blue-500 focus:border-blue-500 block w-full min-w-0 rounded-md sm:text-sm border-gray-300"/>
        </div>
        {fieldHint ? <p class="mt-2 text-sm text-gray-500">{fieldHint}</p> : undefined}
      </div>
    )
  }
}
