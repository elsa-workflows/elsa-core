import {Component, Event, EventEmitter, Listen, h, Prop, Host} from '@stencil/core';
import {leave, toggle} from 'el-transition'
import {groupBy, sortBy, map} from 'lodash';
import {DropdownButtonItem, DropdownButtonOrigin} from "./models";

@Component({
  tag: 'elsa-dropdown-button',
  shadow: false,
})
export class DropdownButton {
  @Prop() public text: string;
  @Prop() public icon?: any;
  @Prop() public handler?: () => void;
  @Prop() public origin: DropdownButtonOrigin = DropdownButtonOrigin.TopLeft;
  @Prop() public items: Array<DropdownButtonItem> = [];
  @Prop() public theme: ('Primary' | 'Secondary') = 'Primary';
  @Event() public itemSelected: EventEmitter<DropdownButtonItem>
  @Event() public menuOpened: EventEmitter<void>

  private contextMenu: HTMLElement;
  private element: HTMLElement;

  public render() {
    const buttonClass = this.theme == 'Secondary' ? 'border-gray-300 bg-white text-gray-700 hover:bg-gray-50 focus:ring-blue-500 hover:border-blue-500' : 'border-blue-600 bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-600 hover:border-blue-700';
    const arrowClass = this.theme == 'Secondary' ? 'border-gray-300 bg-white text-gray-700 hover:bg-gray-50 hover:border-blue-500' : 'border-blue-600 bg-blue-600 text-white hover:bg-blue-700 hover:border-blue-700';
    const handler = this.handler ?? (() => this.toggleMenu());

    return (
      <Host class="block" ref={el => this.element = el}>
        <span class="relative z-0 inline-flex shadow-sm rounded-md">
          <button type="button"
                  class={`relative inline-flex items-center px-4 py-2 rounded-l-md border text-sm font-medium focus:z-10 focus:outline-none ${buttonClass}`}
                  onClick={handler}>
            {this.renderIcon()}
            {this.text}
          </button>
          <div class="-ml-px block">
            <button type="button"
                    class={`relative inline-flex items-center px-2 py-2 rounded-r-md border text-sm font-medium focus:z-10 focus:outline-none ${arrowClass}`}
                    onClick={() => this.toggleMenu()}
                    aria-expanded="true" aria-haspopup="true">
              <svg class="h-5 w-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/>
              </svg>
            </button>
            {this.renderMenu()}
          </div>
        </span>
      </Host>
    );
  }

  private renderMenu() {
    if (this.items.length == 0)
      return;

    const originClass = this.getOriginClass();

    return <div ref={el => this.contextMenu = el}
                data-transition-enter="transition ease-out duration-100"
                data-transition-enter-start="transform opacity-0 scale-95"
                data-transition-leave="transition ease-in duration-75"
                data-transition-leave-start="transform opacity-100 scale-100"
                data-transition-leave-end="transform opacity-0 scale-95"
                class={`hidden ${originClass} z-10 absolute mt-2 w-56 rounded-md shadow-lg bg-white ring-1 ring-black ring-opacity-5`}>
      <div class="py-1" role="menu" aria-orientation="vertical">
        {this.renderItems()}
      </div>
    </div>;
  }

  private renderItems() {

    const groups = groupBy(this.items, x => x.group ?? 0);

    return <div class="divide-y divide-gray-100 focus:outline-none" role="menu" aria-orientation="vertical" aria-labelledby="option-menu">
      {map(groups, menuItemGroup => {

        return <div class="py-1" role="none">
          {menuItemGroup.map(menuItem => {

            const selectedCssClass = menuItem.isSelected ? 'bg-blue-600 hover:bg-blue-700 text-white' : 'hover:bg-gray-100 text-gray-700 hover:text-gray-900';
            return (
              <div class="py-1" role="none">
                <a href="#" onClick={e => this.onItemClick(e, menuItem)}
                   class={`block px-4 py-2 text-sm ${selectedCssClass}`}
                   role="menuitem">
                  {menuItem.text}
                </a>
              </div>);
          })}

        </div>
      })}
    </div>
  }

  private renderIcon = () => this.icon ? this.icon : undefined;

  private closeContextMenu() {
    if (!!this.contextMenu)
      leave(this.contextMenu);
  }

  private toggleMenu() {
    if (!!this.contextMenu) {
      toggle(this.contextMenu);
      this.menuOpened.emit();
    }
  }

  private getOriginClass(): string {
    switch (this.origin) {
      case DropdownButtonOrigin.TopLeft:
        return 'left-0 origin-top-left';
      case DropdownButtonOrigin.TopRight:
      default:
        return 'right-0 origin-top-right';
    }
  }

  private async onItemClick(e: Event, menuItem: DropdownButtonItem) {
    e.preventDefault();

    if (!!menuItem.handler)
      menuItem.handler();

    this.itemSelected.emit(menuItem);
    this.closeContextMenu();
  }

  @Listen('click', {target: 'window'})
  private onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    if (!this.element.contains(target))
      this.closeContextMenu();
  }
}
