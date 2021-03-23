import {Component, Event, EventEmitter, h, Host, Prop, State} from '@stencil/core';
import {leave, toggle} from 'el-transition'
import {registerClickOutside} from "stencil-click-outside";
import {ActivityIcon} from '../../../icons/activity-icon';
import {ActivityModel, ActivityDesignDisplayContext, EventTypes} from "../../../../models";
import {eventBus} from '../../../../services/event-bus';

@Component({
  tag: 'elsa-designer-tree-activity',
  styleUrl: 'elsa-designer-tree-activity.css',
  shadow: false,
})
export class ElsaDesignerTreeActivity {

  defaultIconClass = "h-10 w-10 text-blue-500";

  @Prop() displayContext: ActivityDesignDisplayContext
  @Prop() icon: string
  @Event({eventName: 'remove-activity', bubbles: true}) removeActivityEmitter: EventEmitter<ActivityModel>;
  @Event({eventName: 'edit-activity', bubbles: true}) editActivityEmitter: EventEmitter<ActivityModel>;
  @State() showMenu: boolean
  contextMenu: HTMLElement;
  el: HTMLElement;

  closeContextMenu() {
    leave(this.contextMenu);
  }

  toggleMenu() {
    toggle(this.contextMenu);
  }

  onEditActivityClick(e: Event) {
    e.preventDefault();
    this.closeContextMenu();
    this.editActivityEmitter.emit(this.displayContext.activityModel);
  }

  onDeleteActivityClick(e: Event) {
    e.preventDefault();
    this.closeContextMenu();
    this.removeActivityEmitter.emit(this.displayContext.activityModel);
  }

  render() {
    const displayContext = this.displayContext;
    const activity = displayContext.activityModel;
    const activityId = activity.activityId;
    const displayName = activity.displayName && activity.displayName.length > 0 ? activity.displayName : activity.name && activity.name.length > 0 ? activity.name : activity.type

    return (
      <Host id={`activity-${activityId}`} 
            onDblClick={e => this.onEditActivityClick(e)} 
            class="activity border-2 border-solid border-white rounded bg-white text-left text-black text-lg hover:border-blue-600 select-none max-w-md shadow-sm relative">
        <div class="p-5">
          <div class="flex justify-between space-x-8">
            <div class="flex-shrink-0">
              {displayContext.activityIcon}
            </div>
            <div class="flex-1 font-medium leading-8">
              <p>{displayName}</p>
            </div>
            <div class="context-menu-wrapper flex-shrink-0" ref={el => registerClickOutside(this, el, this.closeContextMenu)}>
              <button onClick={() => this.toggleMenu()} aria-haspopup="true"
                      class="w-8 h-8 inline-flex items-center justify-center text-gray-400 rounded-full bg-transparent hover:text-gray-500 focus:outline-none focus:text-gray-500 focus:bg-gray-100 transition ease-in-out duration-150">
                <svg class="h-6 w-6 text-gray-400" width="24" height="24" viewBox="0 0 24 24" stroke-width="2"
                     stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                  <path stroke="none" d="M0 0h24v24H0z"/>
                  <circle cx="5" cy="12" r="1"/>
                  <circle cx="12" cy="12" r="1"/>
                  <circle cx="19" cy="12" r="1"/>
                </svg>
              </button>
              <div data-transition-enter="transition ease-out duration-100"
                   data-transition-enter-start="transform opacity-0 scale-95"
                   data-transition-enter-end="transform opacity-100 scale-100"
                   data-transition-leave="transition ease-in duration-75"
                   data-transition-leave-start="transform opacity-100 scale-100"
                   data-transition-leave-end="transform opacity-0 scale-95"
                   class="hidden z-10 mx-3 origin-top-left absolute -right-48 top-3 w-48 mt-1 rounded-md shadow-lg"
                   ref={el => this.contextMenu = el}
              >
                <div class="rounded-md bg-white shadow-xs" role="menu" aria-orientation="vertical"
                     aria-labelledby="pinned-project-options-menu-0">
                  <div class="py-1">
                    <a href="#"
                       onClick={e => this.onEditActivityClick(e)}
                       class="block px-4 py-2 text-sm leading-5 text-gray-700 hover:bg-gray-100 hover:text-gray-900 focus:outline-none focus:bg-gray-100 focus:text-gray-900"
                       role="menuitem">Edit</a>
                  </div>
                  <div class="border-t border-gray-100"/>
                  <div class="py-1">
                    <a href="#"
                       onClick={e => this.onDeleteActivityClick(e)}
                       class="block px-4 py-2 text-sm leading-5 text-gray-700 hover:bg-gray-100 hover:text-gray-900 focus:outline-none focus:bg-gray-100 focus:text-gray-900"
                       role="menuitem">Delete</a>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        {this.renderBody()}
      </Host>
    );
  }

  renderBody() {
    if (!this.displayContext.bodyDisplay)
      return undefined;

    return (
      <div class="p-6 text-gray-400 text-sm border-t border-t-solid">
        {this.displayContext.bodyDisplay}
      </div>
    )
  }
}
