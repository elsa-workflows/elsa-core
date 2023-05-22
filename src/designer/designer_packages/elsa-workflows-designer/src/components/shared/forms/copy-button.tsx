import { Component, State, Prop, h } from '@stencil/core';
import { copyTextToClipboard } from '../../../utils';

@Component({
  tag: 'elsa-copy-button',
  shadow: false,
})
export class ElsaCopyButton {
  @State() public isCopied = false;
  @Prop() public value: string = '';

  private copyToClipboard = () => {
    this.isCopied = true;
    copyTextToClipboard(this.value);
    setTimeout(() => {
      this.isCopied = false;
    }, 500);
  };

  public render() {
    return (
      <a href="#" class="tw-ml-2 tw-h-6 tw-w-6 tw-inline-block tw-text-blue-500 hover:tw-text-blue-300" title="Copy value">
        {!this.isCopied ? (
          <svg
            onClick={() => this.copyToClipboard()}
            width="24"
            height="24"
            viewBox="0 0 24 24"
            stroke-width="2"
            stroke="currentColor"
            fill="none"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <path stroke="none" d="M0 0h24v24H0z" />
            <rect x="8" y="8" width="12" height="12" rx="2" />
            <path d="M16 8v-2a2 2 0 0 0 -2 -2h-8a2 2 0 0 0 -2 2v8a2 2 0 0 0 2 2h2" />
          </svg>
        ) : (
          <svg width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <path stroke="none" d="M0 0h24v24H0z" /> <polyline points="9 11 12 14 20 6" /> <path d="M20 12v6a2 2 0 0 1 -2 2h-12a2 2 0 0 1 -2 -2v-12a2 2 0 0 1 2 -2h9" />
          </svg>
        )}
      </a>
    );
  }
}
