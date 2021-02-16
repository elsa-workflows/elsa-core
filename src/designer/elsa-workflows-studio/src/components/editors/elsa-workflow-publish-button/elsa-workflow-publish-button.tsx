import {Component, Host, h, Prop, State, Event, EventEmitter, Watch} from '@stencil/core';
import {enter, leave, toggle} from 'el-transition'
import {registerClickOutside} from "stencil-click-outside";
import {ActivityModel} from "../../../models";

@Component({
  tag: 'elsa-workflow-publish-button',
  styleUrl: 'elsa-workflow-publish-button.css',
  shadow: false,
})
export class ElsaWorkflowPublishButton {

  @Event({bubbles: true}) publishClicked: EventEmitter;

  menu: HTMLElement;

  closeMenu() {
    leave(this.menu);
  }

  toggleMenu() {
    toggle(this.menu);
  }

  onPublishClick(){
    this.publishClicked.emit();
  }

  render() {

    return (
      <Host class="fixed bottom-10 right-12" ref={el => registerClickOutside(this, el, this.closeMenu)}>
        <span class="relative z-0 inline-flex shadow-sm rounded-md">
          <button type="button"
                  onClick={() => this.onPublishClick()}
                  class="relative inline-flex items-center px-4 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500">
            Publish
          </button>
          <span x-data="{ open: true }" class="-ml-px relative block">
            <button onClick={() => this.toggleMenu()} id="option-menu" type="button"
                    class="relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500">
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
              <div class="py-1" role="menu" aria-orientation="vertical" aria-labelledby="option-menu">

                <a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem">
                  Export
                </a>

                <a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem">
                  Import
                </a>

                {/*<a href="#" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900" role="menuitem">*/}
                {/*  Publish later*/}
                {/*</a>*/}

              </div>
            </div>
          </span>
        </span>
      </Host>
    );
  }
}
