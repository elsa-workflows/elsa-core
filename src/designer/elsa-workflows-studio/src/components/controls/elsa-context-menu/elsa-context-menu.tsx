import {Component, h, Listen, Prop} from '@stencil/core';
import {RouterHistory} from "@stencil/router";
import {leave, toggle} from 'el-transition'
import {MenuItem} from "./models";

@Component({
  tag: 'elsa-context-menu',
  shadow: false,
})
export class ElsaContextMenu {
  @Prop() history: RouterHistory;
  @Prop() menuItems: Array<MenuItem> = [];

  navigate: (path: string) => void;
  contextMenu: HTMLElement;
  element: HTMLElement;

  componentWillLoad() {
    if (!!this.history)
      this.navigate = this.history.push;
    else
      this.navigate = path => document.location.href = path;
  }

  @Listen('click', {target: 'window'})
  onWindowClicked(event: Event){
    const target = event.target as HTMLElement;

    if (!this.element.contains(target))
      this.closeContextMenu();
  }

  closeContextMenu() {
    if (!!this.contextMenu)
      leave(this.contextMenu);
  }

  toggleMenu() {
    toggle(this.contextMenu);
  }

  async onMenuItemClick(e: Event, menuItem: MenuItem) {
    e.preventDefault();

    if (!!menuItem.anchorUrl) {
      this.navigate(menuItem.anchorUrl);
    } else if (!!menuItem.clickHandler) {
      menuItem.clickHandler(e);
    }

    this.closeContextMenu();
  }

  render() {
    return (
      <div class="elsa-relative elsa-flex elsa-justify-end elsa-items-center" ref={el => this.element = el}>
        <button onClick={() => this.toggleMenu()} aria-has-popup="true" type="button"
                class="elsa-w-8 elsa-h-8 elsa-inline-flex elsa-items-center elsa-justify-center elsa-text-gray-400 elsa-rounded-full elsa-bg-transparent hover:elsa-text-gray-500 focus:elsa-outline-none focus:elsa-text-gray-500 focus:elsa-bg-gray-100 elsa-transition elsa-ease-in-out elsa-duration-150">
          <svg class="elsa-w-5 elsa-h-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
            <path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z"/>
          </svg>
        </button>
        <div ref={el => this.contextMenu = el}
             data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
             data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
             data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
             data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
             data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"
             class="hidden elsa-z-10 elsa-mx-3 elsa-origin-top-right elsa-absolute elsa-right-7 elsa-top-0 elsa-w-48 elsa-mt-1 elsa-rounded-md elsa-shadow-lg">
          <div class="elsa-rounded-md elsa-bg-white elsa-shadow-xs" role="menu" aria-orientation="vertical" aria-labelledby="project-options-menu-0">
            {this.menuItems.map(menuItem => {
              const anchorUrl = menuItem.anchorUrl || '#';
              return (
                <div class="elsa-py-1">
                  <a href={anchorUrl}
                     onClick={e => this.onMenuItemClick(e, menuItem)}
                     class="elsa-group elsa-flex elsa-items-center elsa-px-4 elsa-py-2 elsa-text-sm elsa-leading-5 elsa-text-gray-700 hover:elsa-bg-gray-100 hover:elsa-text-gray-900 focus:elsa-outline-none focus:elsa-bg-gray-100 focus:elsa-text-gray-900"
                     role="menuitem">
                    {menuItem.icon ? <span class="elsa-mr-3 ">{menuItem.icon}</span> : undefined}
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
