import {Component, Event, h, Host, State} from '@stencil/core';
import {eventBus} from '../../../../services/event-bus';
import {ActivityDescriptor, ActivityDescriptorDisplayContext, ActivityTraits, EventTypes} from "../../../../models";
import state from '../../../../utils/store';
import '../../../../utils/utils';
import {ActivityIcon} from "../../../icons/activity-icon";

@Component({
  tag: 'elsa-activity-picker-modal',
  shadow: false,
})
export class ElsaActivityPickerModal {

  @State() selectedTrait: number = 7;
  @State() selectedCategory: string = 'All';
  @State() searchText: string;
  dialog: HTMLElsaModalDialogElement;
  categories: Array<string> = [];
  filteredActivityDescriptorDisplayContexts: Array<ActivityDescriptorDisplayContext> = [];

  connectedCallback() {
    eventBus.on(EventTypes.ShowActivityPicker, this.onShowActivityPicker);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.ShowActivityPicker, this.onShowActivityPicker);
  }

  onShowActivityPicker = async () => {
    await this.dialog.show(true);
  };

  componentWillRender() {
    const activityDescriptors: Array<ActivityDescriptor> = state.activityDescriptors;
    this.categories = ['All', ...activityDescriptors.map(x => x.category).distinct().sort()];
    const searchText = this.searchText ? this.searchText.toLowerCase() : '';
    let filteredActivityDescriptors = activityDescriptors;

    if (searchText.length > 0) {
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
    }
    else {
      filteredActivityDescriptors = filteredActivityDescriptors.filter(x => (x.traits & this.selectedTrait) == x.traits)
      filteredActivityDescriptors = !this.selectedCategory || this.selectedCategory == 'All' ? filteredActivityDescriptors : filteredActivityDescriptors.filter(x => x.category == this.selectedCategory);
    }

    this.filteredActivityDescriptorDisplayContexts = filteredActivityDescriptors.map(x => {
      const color = (x.traits &= ActivityTraits.Trigger) == ActivityTraits.Trigger ? 'rose' : (x.traits &= ActivityTraits.Job) == ActivityTraits.Job ? 'yellow' : 'sky';
      return {
        activityDescriptor: x,
        activityIcon: <ActivityIcon color={color}/>
      };
    });

    for (const context of this.filteredActivityDescriptorDisplayContexts)
      eventBus.emit(EventTypes.ActivityDescriptorDisplaying, this, context);
  }

  selectTrait(trait: number) {
    this.selectedTrait = trait;
  }

  selectCategory(category: string) {
    this.selectedCategory = category;
  }

  onTraitClick(e: MouseEvent, trait: number) {
    e.preventDefault();
    this.selectTrait(trait);
  }

  onCategoryClick(e: MouseEvent, category: string) {
    e.preventDefault();
    this.selectCategory(category);
  }

  onSearchTextChange(e: any) {
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
    const selectedCategoryClass = 'elsa-bg-gray-100 elsa-text-gray-900 elsa-flex';
    const defaultCategoryClass = 'elsa-text-gray-600 hover:elsa-bg-gray-50 hover:elsa-text-gray-900';
    const filteredDisplayContexts = this.filteredActivityDescriptorDisplayContexts;
    const categories = this.categories;

    return (
      <Host class="elsa-block">
        <elsa-modal-dialog ref={el => this.dialog = el}>
          <div slot="content" class="elsa-py-8">
            <div class="elsa-flex">
              <div class="elsa-px-8">
                <nav class="elsa-space-y-1" aria-label="Sidebar">
                  {categories.map(category => (
                    <a href="#" onClick={e => this.onCategoryClick(e, category)}
                       class={`${category == this.selectedCategory ? selectedCategoryClass : defaultCategoryClass} elsa-text-gray-600 hover:elsa-bg-gray-50 hover:elsa-text-gray-900 elsa-flex elsa-items-center elsa-px-3 elsa-py-2 elsa-text-sm elsa-font-medium elsa-rounded-md`}>
                    <span class="elsa-truncate">
                      {category}
                    </span>
                    </a>
                  ))}
                </nav>
              </div>
              <div class="elsa-flex-1 elsa-pr-8">
                <div class="elsa-p-0 elsa-mb-6">
                  <div class="elsa-relative elsa-rounded-md elsa-shadow-sm">
                    <div class="elsa-absolute elsa-inset-y-0 elsa-left-0 elsa-pl-3 elsa-flex elsa-items-center elsa-pointer-events-none">
                      <svg class="elsa-h-6 elsa-w-6 elsa-text-gray-400" width="24" height="24" viewBox="0 0 24 24"
                           stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round"
                           stroke-linejoin="round">
                        <path stroke="none" d="M0 0h24v24H0z"/>
                        <circle cx="10" cy="10" r="7"/>
                        <line x1="21" y1="21" x2="15" y2="15"/>
                      </svg>
                    </div>
                    <input type="text" value={this.searchText} onInput={e => this.onSearchTextChange(e)}
                           class="form-input elsa-block elsa-w-full elsa-pl-10 sm:elsa-text-sm sm:elsa-leading-5 focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-rounded-md elsa-border-gray-300"
                           placeholder="Search activities"/>
                  </div>
                </div>

                <div class="elsa-max-w-4xl elsa-mx-auto elsa-p-0">

                  {categories.map(category => {
                    const displayContexts = filteredDisplayContexts.filter(x => x.activityDescriptor.category == category);

                    if (displayContexts.length == 0)
                      return undefined;

                    return (
                      <div>
                        <h2 class="elsa-my-4 elsa-text-lg elsa-leading-6 elsa-font-medium">{category}</h2>
                        <div class="elsa-divide-y elsa-divide-gray-200 sm:elsa-divide-y-0 sm:elsa-grid sm:elsa-grid-cols-2 sm:elsa-gap-px">
                          {displayContexts.map(displayContext => (
                            <a href="#" onClick={e => this.onActivityClick(e, displayContext.activityDescriptor)} class="elsa-relative elsa-rounded elsa-group elsa-p-6 focus-within:elsa-ring-2 focus-within:elsa-ring-inset focus-within:elsa-ring-blue-500">
                              <div class="elsa-flex elsa-space-x-10">
                                <div class="elsa-flex elsa-flex-0 elsa-items-center">
                                  <div innerHTML={displayContext.activityIcon}>
                                  </div>
                                </div>
                                <div class="elsa-flex-1 elsa-mt-2">
                                  <h3 class="elsa-text-lg elsa-font-medium">
                                    <a href="#" class="focus:elsa-outline-none">
                                      <span class="elsa-absolute elsa-inset-0" aria-hidden="true"/>
                                      {displayContext.activityDescriptor.displayName}
                                    </a>
                                  </h3>
                                  <p class="elsa-mt-2 elsa-text-sm elsa-text-gray-500">
                                    {displayContext.activityDescriptor.description}
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
            <div class="elsa-bg-gray-50 elsa-px-4 elsa-py-3 sm:elsa-px-6 sm:elsa-flex sm:elsa-flex-row-reverse">
              <button type="button"
                      onClick={() => this.onCancelClick()}
                      class="elsa-mt-3 elsa-w-full elsa-inline-flex elsa-justify-center elsa-rounded-md elsa-border elsa-border-gray-300 elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-bg-white elsa-text-base elsa-font-medium elsa-text-gray-700 hover:elsa-bg-gray-50 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500 sm:elsa-mt-0 sm:elsa-ml-3 sm:elsa-w-auto sm:elsa-text-sm">
                Cancel
              </button>
            </div>
          </div>
        </elsa-modal-dialog>
      </Host>
    );
  }
}
