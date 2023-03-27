import {FunctionalComponent, h} from "@stencil/core";

export const PlayButtonIcon: FunctionalComponent = () =>
  <svg
    class="-ml-1 mr-2 h-5 w-5"
    width="24" height="24" viewBox="0 0 24 24"
    stroke-width="2" stroke="currentColor" fill="transparent" stroke-linecap="round"
    stroke-linejoin="round">
    <polygon points="5 3 19 12 5 21 5 3" />
  </svg>;
