import {Component, Event, EventEmitter, Listen, h, Prop} from '@stencil/core';
import {leave, toggle} from 'el-transition'
import {DropdownButtonItem, DropdownButtonOrigin} from "./models";

@Component({
    tag: 'elsa-dropdown-button',
    styleUrl: 'elsa-dropdown-button.css',
    shadow: false,
})
export class ElsaContextMenu {
    @Prop() text: string;
    @Prop() icon?: any;
    @Prop() btnClass?: string = " elsa-w-full elsa-bg-white elsa-border elsa-border-gray-300 elsa-rounded-md elsa-shadow-sm elsa-px-4 elsa-py-2 elsa-inline-flex elsa-justify-center elsa-text-sm elsa-font-medium elsa-text-gray-700 hover:elsa-bg-gray-50 focus:elsa-outline-none focus:elsa-ring-2 focus:elsa-ring-offset-2 focus:elsa-ring-blue-500"
    @Prop() origin: DropdownButtonOrigin = DropdownButtonOrigin.TopLeft;
    @Prop() items: Array<DropdownButtonItem> = [];

    @Event() itemSelected: EventEmitter<DropdownButtonItem>

    contextMenu: HTMLElement;
    element: HTMLElement;

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
        if (!!this.contextMenu)
            toggle(this.contextMenu);
    }

    getOriginClass(): string {
        switch (this.origin) {
            case DropdownButtonOrigin.TopLeft:
                return `elsa-left-0 elsa-origin-top-left`;
            case DropdownButtonOrigin.TopRight:
            default:
                return 'elsa-right-0 elsa-origin-top-right';
        }
    }

    async onItemClick(e: Event, menuItem: DropdownButtonItem) {
        e.preventDefault();
        this.itemSelected.emit(menuItem);
        this.closeContextMenu();
    }

    render() {
        return (
            <div class="elsa-relative" ref={el => this.element = el}>
                <button onClick={e => this.toggleMenu()} type="button"
                        class={this.btnClass}
                        aria-haspopup="true" aria-expanded="false">
                    {this.renderIcon()}
                    {this.text}
                    <svg class="elsa-ml-2.5 -elsa-elsa-mr-1.5 elsa-h-5 elsa-w-5 elsa-text-gray-400" x-description="Heroicon name: chevron-down" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd"/>
                    </svg>
                </button>
                {this.renderMenu()}
            </div>
        );
    }

    renderMenu() {
        if (this.items.length == 0)
            return;

        const originClass = this.getOriginClass();

        return <div ref={el => this.contextMenu = el}
                    data-transition-enter="elsa-transition elsa-ease-out elsa-duration-100"
                    data-transition-enter-start="elsa-transform elsa-opacity-0 elsa-scale-95"
                    data-transition-leave="elsa-transition elsa-ease-in elsa-duration-75"
                    data-transition-leave-start="elsa-transform elsa-opacity-100 elsa-scale-100"
                    data-transition-leave-end="elsa-transform elsa-opacity-0 elsa-scale-95"            
                    class={`hidden ${originClass} elsa-z-10 elsa-absolute elsa-mt-2 elsa-w-56 elsa-rounded-md elsa-shadow-lg elsa-bg-white elsa-ring-1 elsa-ring-black elsa-ring-opacity-5`}>
            <div class="elsa-py-1" role="menu" aria-orientation="vertical">
                {this.renderItems()}
            </div>
        </div>;
    }

    renderItems() {
        return this.items.map(item => {
            const selectedCssClass = item.isSelected ? "elsa-bg-blue-600 hover:elsa-bg-blue-700 elsa-text-white" : "hover:elsa-bg-gray-100 elsa-text-gray-700 hover:elsa-text-gray-900";

            return !!item.url
                ? <stencil-route-link onClick={e => this.closeContextMenu()} url={item.url} anchorClass={`elsa-block elsa-px-4 elsa-py-2 elsa-text-sm ${selectedCssClass} elsa-cursor-pointer`} role="menuitem">{item.text}</stencil-route-link>
                : <a href="#" onClick={e => this.onItemClick(e, item)} class={`elsa-block elsa-px-4 elsa-py-2 elsa-text-sm ${selectedCssClass}`} role="menuitem">{item.text}</a>;
        })
    }

    renderIcon() {
        if (!this.icon)
            return;

        return this.icon;
    }
}
