import {Component, Host, h, State} from '@stencil/core';
import {eventBus} from '../../../utils/event-bus';
import state from '../../../utils/store';
import {ActivityDescriptor, ActivityModel, ActivityPropertyDescriptor, EventTypes} from "../../../models";
import {propertyDisplayManager} from '../../../services/property-display-manager';

@Component({
  tag: 'elsa-activity-editor-modal',
  styleUrl: 'elsa-activity-editor-modal.css',
  shadow: false,
})
export class ElsaActivityPickerModal {

  @State() activityModel: ActivityModel;
  @State() activityDescriptor: ActivityDescriptor;
  dialog: HTMLElsaModalDialogElement;

  componentDidLoad() {
    eventBus.on(EventTypes.ShowActivityEditor, async (activity: ActivityModel, animate: boolean) => {
      this.activityModel = {...activity, properties: activity.properties || []};
      this.activityDescriptor = state.activityDescriptors.find(x => x.type == activity.type);
      await this.dialog.show(animate);
    });
  }

  updateActivity(formData: FormData){
    const activity = this.activityModel;
    const activityDescriptor = this.activityDescriptor;
    const properties: Array<ActivityPropertyDescriptor> = activityDescriptor.properties;

    for (const property of properties)
      propertyDisplayManager.update(activity, property, formData);
  }

  async onCancelClick() {
    await this.dialog.hide(true);
  }

  async onSubmit(e: Event) {
    e.preventDefault();
    const form: any = e.target;
    const formData = new FormData(form);
    this.updateActivity(formData);
    eventBus.emit(EventTypes.UpdateActivity, this, this.activityModel);
    await this.dialog.hide(true);
  }

  render() {
    const activityModel: ActivityModel = this.activityModel || {type: '', activityId: '', outcomes: [], properties: []};
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
                      {propertyDescriptors.map(property => this.renderPropertyEditor(activityModel, property))}
                    </div>
                  </div>

                </div>
              </div>
              <div class="pt-5">
                <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
                  <button type="submit"
                          class="ml-3 inline-flex justify-center py-2 px-4 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
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

  renderPropertyEditor(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    return propertyDisplayManager.display(activity, property);
  }
}
