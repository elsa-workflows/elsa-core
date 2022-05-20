import {Component, h, Prop} from '@stencil/core';
import {Button} from "./models";

@Component({
  tag: 'elsa-button-group',
  shadow: false,
})
export class ButtonGroup {
  @Prop({mutable: true}) buttons: Array<Button> = [];

  element: HTMLElement;

  render() {
    return (
      <span class="relative z-0 inline-flex shadow-sm rounded-md">
        {this.renderButtons()}
      </span>
    );
  }

  renderButtons = () => {
    const buttons = this.buttons;

    if (buttons.length == 0)
      return;

    return <div class="py-1">
      {buttons.map(this.renderButton)}
    </div>
  };

  renderButton = (button: Button, index: number) => {

    const buttons = this.buttons;
    const cssClass = buttons.length == 1 ? `relative rounded-l-md rounded-r-md`: index == 0 ? 'relative rounded-l-md' : index == this.buttons.length - 1 ? '-ml-px rounded-r-md' : '-ml-px';

    return <button onClick={e => ButtonGroup.onButtonClick(e, button)} type="button"
                   class={`${cssClass} inline-flex items-center px-4 py-2 border border-gray-300 bg-white text-sm font-medium text-gray-700 hover:bg-gray-50 focus:z-10 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500`}>
      {button.text}
    </button>;
  }

  private static async onButtonClick(e: MouseEvent, button: Button) {
    e.preventDefault();

    if (!!button.clickHandler)
      button.clickHandler(e);
  }
}
