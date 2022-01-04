import {FunctionalComponent, h} from "@stencil/core";

export const PageSizeIcon: FunctionalComponent = () =>
  <svg class="h-5 w-5 text-gray-400 mr-2" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
    <path stroke="none" d="M0 0h24v24H0z"/>
    <line x1="9" y1="6" x2="20" y2="6"/>
    <line x1="9" y1="12" x2="20" y2="12"/>
    <line x1="9" y1="18" x2="20" y2="18"/>
    <line x1="5" y1="6" x2="5" y2="6.01"/>
    <line x1="5" y1="12" x2="5" y2="12.01"/>
    <line x1="5" y1="18" x2="5" y2="18.01"/>
  </svg>;
