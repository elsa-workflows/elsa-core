import {FunctionalComponent, h} from "@stencil/core";

export const PlusButtonIcon: FunctionalComponent = () =>
  <svg
    class="-tw-ml-1 tw-mr-2 tw-h-5 tw-w-5"
    width="24" height="24" viewBox="0 0 24 24"
    stroke-width="2" stroke="currentColor" fill="transparent" stroke-linecap="round"
    stroke-linejoin="round">
    <path stroke="none" d="M0 0h24v24H0z"/>
    <line x1="12" y1="5" x2="12" y2="19"/>
    <line x1="5" y1="12" x2="19" y2="12"/>
  </svg>;
