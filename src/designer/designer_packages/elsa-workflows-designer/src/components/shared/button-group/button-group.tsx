import {Component, h, Prop} from '@stencil/core';
import {Button} from "./models";

// noinspection HtmlUnknownAttribute
@Component({
  tag: 'elsa-button-group',
  shadow: false,
})
export class ButtonGroup {
  @Prop({mutable: true}) buttons: Array<Button> = [];

  element: HTMLElement;

  render() {
    return (
      <span class="tw-relative tw-z-0 tw-inline-flex tw-rounded-md">
        {this.renderButtons()}
      </span>
    );
  }

  renderButtons = () => {
    const buttons = this.buttons;

    if (buttons.length == 0)
      return;

    return <div class="tw-py-1">
      {buttons.map(this.renderButton)}
    </div>
  };

  renderButton = (button: Button, index: number) => {

    const buttons = this.buttons;
    const cssClass = buttons.length == 1 ? `tw-relative tw-rounded-l-md tw-rounded-r-md`: index == 0 ? 'tw-relative tw-rounded-l-md' : index == this.buttons.length - 1 ? '-tw-ml-px tw-rounded-r-md' : '-tw-ml-px';

    return <button onClick={e => ButtonGroup.onButtonClick(e, button)} type="button"
                   class={`${cssClass} tw-inline-flex tw-items-center tw-px-4 tw-py-2 tw-border tw-border-gray-300 tw-bg-white tw-text-sm tw-font-medium tw-text-gray-700 hover:tw-bg-gray-50 focus:tw-z-10 focus:tw-outline-none focus:tw-ring-1 focus:tw-ring-blue-500 focus:tw-border-blue-500`}>
      {button.text}
    </button>;
  }

  private static async onButtonClick(e: MouseEvent, button: Button) {
    e.preventDefault();

    if (!!button.clickHandler)
      button.clickHandler(e);
  }
}
