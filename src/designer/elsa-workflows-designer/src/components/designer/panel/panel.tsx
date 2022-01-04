import {Component, Event, EventEmitter, h, Host, Prop, State} from '@stencil/core';
import {PanelPosition, PanelStateChangedArgs} from './models';

@Component({
  tag: 'elsa-panel',
  styleUrl: 'panel.scss',
})
export class Panel {
  @Prop() position: PanelPosition = PanelPosition.Left;
  @Event() expandedStateChanged: EventEmitter<PanelStateChangedArgs>;
  @State() isExpanded: boolean = true;

  private onToggleClick = () => {
    this.isExpanded = !this.isExpanded;
    this.expandedStateChanged.emit({expanded: this.isExpanded});
  };

  render() {
    const isExpanded = this.isExpanded;
    const position = this.position;
    const stateClass = isExpanded ? 'panel-state-expanded' : 'panel-state-collapsed';

    const containerClassMap = [];
    containerClassMap[PanelPosition.Left] = 'panel-position-left left-0 top-0 bottom-0 border-r';
    containerClassMap[PanelPosition.Top] = 'panel-position-top left-0 top-0 right-0 border-b';
    containerClassMap[PanelPosition.Right] = 'panel-position-right right-0 top-0 bottom-0 border-l';
    const containerCssClass = containerClassMap[position];

    const toggleClassMap = {};
    toggleClassMap[PanelPosition.Left] = 'panel-toggle-left';
    toggleClassMap[PanelPosition.Top] = 'panel-toggle-top';
    toggleClassMap[PanelPosition.Right] = 'panel-toggle-right';
    toggleClassMap[PanelPosition.Bottom] = 'panel-toggle-bottom';

    const toggleCssClass = toggleClassMap[position];

    return (
      <Host
        class={`panel absolute transition-all duration-200 ease-in-out bg-white z-10 ${containerCssClass} ${stateClass}`}>

        <div class="panel-content-container">
          <slot/>
        </div>

        <div class={`text-white ${toggleCssClass}`} onClick={() => this.onToggleClick()}>
          <svg class="h-6 w-6 text-gray-700" width="24" height="24" viewBox="0 0 24 24" stroke-width="2"
               stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <path stroke="none" d="M0 0h24v24H0z"/>
            <polyline points="9 6 15 12 9 18"/>
          </svg>
        </div>
      </Host>
    );
  }
}
