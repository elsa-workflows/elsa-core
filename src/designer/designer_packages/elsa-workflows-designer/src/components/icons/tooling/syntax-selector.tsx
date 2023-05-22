import {FunctionalComponent, h} from "@stencil/core";

export const SyntaxSelectorIcon: FunctionalComponent = () =>
  <svg class="tw-h-5 tw-w-5 tw-text-gray-400" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
    <path stroke="none" d="M0 0h24v24H0z"/>
    <circle cx="12" cy="12" r="9"/>
    <line x1="8" y1="12" x2="8" y2="12.01"/>
    <line x1="12" y1="12" x2="12" y2="12.01"/>
    <line x1="16" y1="12" x2="16" y2="12.01"/>
  </svg>;
