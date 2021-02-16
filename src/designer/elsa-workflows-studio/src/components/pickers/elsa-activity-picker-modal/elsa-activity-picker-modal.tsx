import {Component, Host, h, Prop, State, Event, EventEmitter} from '@stencil/core';
import '../../../utils/utils';
import {eventBus} from '../../../utils/event-bus';
import {ActivityDescriptor, EventTypes} from "../../../models";
import state from '../../../utils/store';

@Component({
  tag: 'elsa-activity-picker-modal',
  styleUrl: 'elsa-activity-picker-modal.css',
  shadow: false,
})
export class ElsaActivityPickerModal {

  @State() selectedTrait: number = 7;
  @State() searchText: string;
  dialog: HTMLElsaModalDialogElement;

  componentDidLoad() {
    eventBus.on(EventTypes.ShowActivityPicker, async () => await this.dialog.show(true));
  }

  selectTrait(trait: number) {
    this.selectedTrait = trait;
  }

  onTraitClick(e: MouseEvent, trait: number) {
    e.preventDefault();
    this.selectTrait(trait);
  }

  onSearchTextChange(e: TextEvent) {
    this.searchText = (e.target as HTMLInputElement).value;
  }

  async onCancelClick() {
    await this.dialog.hide(true);
  }

  async onActivityClick(e: Event, activityDescriptor: ActivityDescriptor) {
    e.preventDefault();
    eventBus.emit(EventTypes.ActivityPicked, this, activityDescriptor);
    await this.dialog.hide(false);
  }

  render() {
    const activityDescriptors: Array<ActivityDescriptor> = state.activityDescriptors;
    const categories = activityDescriptors.map(x => x.category).distinct().sort();
    const traits = [{text: 'All', value: 7}, {text: 'Actions', value: 1}, {text: 'Triggers', value: 2}, {text: 'Jobs', value: 4}];
    const selectedTraitClass = 'bg-gray-100 text-gray-900 flex';
    const defaultTraitClass = 'text-gray-600 hover:bg-gray-50 hover:text-gray-900';
    const searchText = this.searchText ? this.searchText.toLowerCase() : '';
    let filteredActivityDescriptors = activityDescriptors;

    filteredActivityDescriptors = filteredActivityDescriptors.filter(x => (x.traits & this.selectedTrait) == x.traits)

    if (searchText.length > 0)
      filteredActivityDescriptors = filteredActivityDescriptors.filter(x => {
        const category = x.category || '';
        const description = x.description || '';
        const displayName = x.displayName || '';
        const type = x.type || '';

        return category.toLowerCase().indexOf(searchText) >= 0
          || description.toLowerCase().indexOf(searchText) >= 0
          || displayName.toLowerCase().indexOf(searchText) >= 0
          || type.toLowerCase().indexOf(searchText) >= 0;
      });

    return (
      <Host>
        <elsa-modal-dialog ref={el => this.dialog = el}>
          <div slot="content" class="py-8">
            <div class="flex">
              <div class="px-8">
                <nav class="space-y-1" aria-label="Sidebar">
                  {traits.map(trait => (
                    <a href="#" onClick={e => this.onTraitClick(e, trait.value)}
                       class={`${trait.value == this.selectedTrait ? selectedTraitClass : defaultTraitClass} text-gray-600 hover:bg-gray-50 hover:text-gray-900 flex items-center px-3 py-2 text-sm font-medium rounded-md`}>
                    <span class="truncate">
                      {trait.text}
                    </span>
                    </a>
                  ))}
                </nav>
              </div>
              <div class="flex-1 pr-8">

                <div class="p-0 mb-6">
                  <div class="relative rounded-md shadow-sm">
                    <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <svg class="h-6 w-6 text-gray-400" width="24" height="24" viewBox="0 0 24 24"
                           stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round"
                           stroke-linejoin="round">
                        <path stroke="none" d="M0 0h24v24H0z"/>
                        <circle cx="10" cy="10" r="7"/>
                        <line x1="21" y1="21" x2="15" y2="15"/>
                      </svg>
                    </div>
                    <input type="text" value={this.searchText} onInput={e => this.onSearchTextChange(e as TextEvent)} class="form-input block w-full pl-10 sm:text-sm sm:leading-5" placeholder="Search activities"/>
                  </div>
                </div>

                <div class="max-w-4xl mx-auto p-0">

                  {categories.map(category => {
                    const activities = filteredActivityDescriptors.filter(x => x.category == category);

                    if (activities.length == 0)
                      return undefined;

                    return (
                      <div>
                        <h2 class="my-4 text-lg leading-6 font-medium">{category}</h2>
                        <div class="divide-y divide-gray-200 sm:divide-y-0 sm:grid sm:grid-cols-2 sm:gap-px">
                          {activities.map(activityDescriptor => (
                            <a href="#" onClick={e => this.onActivityClick(e, activityDescriptor)} class="relative rounded group p-6 focus-within:ring-2 focus-within:ring-inset focus-within:ring-blue-500">
                              <div class="flex space-x-10">
                                <div class="flex-0">
                                  <div>
                                      <span class="rounded-lg inline-flex p-3 bg-blue-50 text-blue-700 ring-4 ring-white">
                                        <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                                          <path stroke="none" d="M0 0h24v24H0z"/>  <polyline points="21 12 17 12 14 20 10 4 7 12 3 12"/>
                                        </svg>
                                      </span>
                                  </div>
                                </div>
                                <div class="flex-1 mt-2">
                                  <h3 class="text-lg font-medium">
                                    <a href="#" class="focus:outline-none">
                                      <span class="absolute inset-0" aria-hidden="true"/>
                                      {activityDescriptor.displayName}
                                    </a>
                                  </h3>
                                  <p class="mt-2 text-sm text-gray-500">
                                    {activityDescriptor.description}
                                  </p>
                                </div>
                              </div>
                            </a>
                          ))}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>
          </div>
          <div slot="buttons">
            <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
              <button type="button"
                      onClick={() => this.onCancelClick()}
                      class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm">
                Cancel
              </button>
            </div>
          </div>
        </elsa-modal-dialog>
      </Host>
    );
  }
}
