import {FunctionalComponent, h} from "@stencil/core";

export const TrashBinButtonIcon: FunctionalComponent = () =>
  <svg
    class="tw-h-5 tw-w-5 tw-text-gray-500"
    width="24" height="24" viewBox="0 0 24 24"
    stroke-width="2" stroke="currentColor" fill="transparent" stroke-linecap="round"
    stroke-linejoin="round">
    <polyline points="3 6 5 6 21 6"/>
    <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>
    <line x1="10" y1="11" x2="10" y2="17"/>
    <line x1="14" y1="11" x2="14" y2="17"/>
  </svg>;
