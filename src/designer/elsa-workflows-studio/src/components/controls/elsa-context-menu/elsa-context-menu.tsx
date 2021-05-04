import {Component, h, Prop} from '@stencil/core';
import {RouterHistory} from "@stencil/router";
import {leave, toggle} from 'el-transition'
import {registerClickOutside} from "stencil-click-outside";
import {MenuItem} from "./models";

@Component({
  tag: 'elsa-context-menu',
  styleUrl: 'elsa-context-menu.css',
  shadow: false,
})
export class ElsaContextMenu {
  @Prop() history: RouterHistory;
  @Prop() menuItems: Array<MenuItem> = [];

  navigate: (path: string) => void;
  contextMenu: HTMLElement;

  componentWillLoad(){
    if(!!this.history)
      this.navigate = this.history.push;
    else
      this.navigate = path => document.location.href = path;
  }
  
  closeContextMenu() {
    leave(this.contextMenu);
  }

  toggleMenu() {
    toggle(this.contextMenu);
  }

  async onMenuItemClick(e: Event, menuItem: MenuItem) {
    e.preventDefault();

    if (!!menuItem.anchorUrl) {
      this.navigate(menuItem.anchorUrl);
    }
    else if (!!menuItem.clickHandler) {
      menuItem.clickHandler(e);
    }

    this.closeContextMenu();
  }

  render() {
    return (
      <div class="relative flex justify-end items-center" ref={el => registerClickOutside(this, el, this.closeContextMenu)}>
        <button onClick={() => this.toggleMenu()} aria-has-popup="true" type="button"
                class="w-8 h-8 inline-flex items-center justify-center text-gray-400 rounded-full bg-transparent hover:text-gray-500 focus:outline-none focus:text-gray-500 focus:bg-gray-100 transition ease-in-out duration-150">
          <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
            <path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z"/>
          </svg>
        </button>
        <div ref={el => this.contextMenu = el}
             data-transition-enter="transition ease-out duration-100"
             data-transition-enter-start="transform opacity-0 scale-95"
             data-transition-leave="transition ease-in duration-75"
             data-transition-leave-start="transform opacity-100 scale-100"
             data-transition-leave-end="transform opacity-0 scale-95"
             class="hidden z-10 mx-3 origin-top-right absolute right-7 top-0 w-48 mt-1 rounded-md shadow-lg">
          <div class="rounded-md bg-white shadow-xs" role="menu" aria-orientation="vertical" aria-labelledby="project-options-menu-0">
            {this.menuItems.map(menuItem => {
              const anchorUrl = menuItem.anchorUrl || '#';
              return (
                <div class="py-1">
                  <a href={anchorUrl}
                     onClick={e => this.onMenuItemClick(e, menuItem)}
                     class="group flex items-center px-4 py-2 text-sm leading-5 text-gray-700 hover:bg-gray-100 hover:text-gray-900 focus:outline-none focus:bg-gray-100 focus:text-gray-900" role="menuitem">
                    {menuItem.icon ? <span class="mr-3 ">{menuItem.icon}</span> : undefined}
                    {menuItem.text}
                  </a>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    );
  }
}
