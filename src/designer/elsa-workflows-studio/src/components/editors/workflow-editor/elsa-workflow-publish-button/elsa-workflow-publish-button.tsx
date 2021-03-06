import {Component, Host, h, Prop, State, Event, EventEmitter, Watch} from '@stencil/core';
import {enter, leave, toggle} from 'el-transition'
import {registerClickOutside} from "stencil-click-outside";
import {ActivityModel} from "../../../../models";

@Component({
  tag: 'elsa-workflow-publish-button',
  styleUrl: 'elsa-workflow-publish-button.css',
  shadow: false,
})
export class ElsaWorkflowPublishButton {

  @Prop() publishing: boolean;
  @Event({bubbles: true}) publishClicked: EventEmitter;

  menu: HTMLElement;

  closeMenu() {
    leave(this.menu);
  }

  toggleMenu() {
    toggle(this.menu);
  }

  async onPublishClick() {
    this.publishClicked.emit();
  }

  render() {

    return (
      <Host class="fixed bottom-10 right-12" ref={el => registerClickOutside(this, el, this.closeMenu)}>
        <span class="relative z-0 inline-flex shadow-sm rounded-md">
          {this.publishing ? this.renderPublishingButton() : this.renderPublishButton()}
          <span x-data="{ open: true }" class="-ml-px relative block">
            <button onClick={() => this.toggleMenu()} id="option-menu" type="button"
                    class="relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500">
              <span class="sr-only">Open options</span>
              <svg class="h-5 w-5" x-description="Heroicon name: solid/chevron-down" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd"/>
              </svg>
            </button>
            <div ref={el => this.menu = el}
                 data-transition-enter="transition ease-out duration-100"
                 data-transition-enter-start="transform opacity-0 scale-95"
                 data-transition-enter-end="transform opacity-100 scale-100"
                 data-transition-leave="transition ease-in duration-75"
                 data-transition-leave-start="transform opacity-100 scale-100"
                 data-transition-leave-end="transform opacity-0 scale-95"
                 class="hidden origin-bottom-right absolute right-0 bottom-10 mb-2 -mr-1 w-56 rounded-md shadow-lg bg-white ring-1 ring-black ring-opacity-5">
              <div class="divide-y divide-gray-100 focus:outline-none" role="menu" aria-orientation="vertical" aria-labelledby="option-menu">

                <div class="py-1" role="none">
                  <a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem">
                    Export
                  </a>

                  <a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem">
                    Import
                  </a>
                </div>

                <div class="py-1" role="none">
                  <a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem">
                    Unpublish
                  </a>
                </div>

              </div>
            </div>
          </span>
        </span>
      </Host>
    );
  }

  renderPublishButton() {
    return (
      <button type="button"
              onClick={() => this.onPublishClick()}
              class="relative inline-flex items-center px-4 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500">

        Publish
      </button>);
  }

  renderPublishingButton() {
    return (
      <button type="button"
              disabled={true}
              class="relative inline-flex items-center px-4 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500">

        <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-blue-400" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
        </svg>
        Publishing
      </button>);
  }
}
