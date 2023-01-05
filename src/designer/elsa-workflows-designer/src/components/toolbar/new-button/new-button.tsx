import {Component, Event, EventEmitter, Host, h, Listen, Prop} from '@stencil/core';
import {leave, toggle} from 'el-transition'
import newButtonItemStore from "../../../data/new-button-item-store";
import {MenuItem, MenuItemGroup} from "../../shared/context-menu/models";

@Component({
  tag: 'elsa-new-button',
  shadow: false,
})
export class NewButton {

  @Event({bubbles: true}) newClicked: EventEmitter;

  menu: HTMLElement;
  element: HTMLElement;

  @Listen('click', {target: 'window'})
  private onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    if (!this.element.contains(target))
      this.closeMenu();
  }

  private closeMenu() {
    leave(this.menu);
  }

  private toggleMenu() {
    toggle(this.menu);
  }

  private onItemClick = (e: MouseEvent, item: MenuItem) => {
    e.preventDefault();

    if (item.clickHandler != null)
      item.clickHandler(e);

    leave(this.menu);
  };

  render() {

    const groups: Array<MenuItemGroup> = newButtonItemStore.groups;

    const mainItem: MenuItem = newButtonItemStore.mainItem ?? {
      text: 'New',
      clickHandler: () => this.toggleMenu()
    };

    return (
      <Host class="block" ref={el => this.element = el}>
        <span class="relative z-0 inline-flex shadow-sm rounded-md">
          <button type="button"
                  onClick={mainItem.clickHandler}
                  class="relative inline-flex items-center px-4 py-2 rounded-l-md border border-blue-600 bg-blue-600 text-sm font-medium text-white hover:bg-blue-700 focus:z-10 focus:outline-none focus:ring-blue-600 hover:border-blue-700">
            {mainItem.text}
          </button>
          <span class="-ml-px relative block">
            <button onClick={() => this.toggleMenu()} id="option-menu" type="button"
                    class="relative inline-flex items-center px-2 py-2 rounded-r-md border border-blue-600 bg-blue-600 text-sm font-medium text-white hover:bg-blue-700 focus:z-10 focus:outline-none hover:border-blue-700">
              <svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
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
                 class="hidden origin-bottom-right absolute right-0 top-10 mb-2 -mr-1 w-56 rounded-md shadow-lg bg-white ring-1 ring-black ring-opacity-5">

              <div class="divide-y divide-gray-100 focus:outline-none" role="menu" aria-orientation="vertical" aria-labelledby="option-menu">
              {groups.map(menuItemGroup => {

                return <div class="py-1" role="none">
                  {menuItemGroup.menuItems.map(menuItem => {
                    return (
                      <div class="py-1" role="none">
                        <a href="#" onClick={e => this.onItemClick(e, menuItem)}
                           class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900"
                           role="menuitem">
                          {menuItem.text}
                        </a>
                      </div>);
                  })}

                </div>
              })}
              </div>
            </div>
          </span>
        </span>
      </Host>
    );
  }
}
