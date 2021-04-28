import {h} from "@stencil/core";

export const InterruptTriggerIcon = props =>
  (
    `<span class="${`rounded-lg inline-flex p-3 bg-light-blue-50 text-light-blue-700 ring-4 ring-white`}">
      <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <path d="M8 16v-4a4 4 0 0 1 8 0v4"/>
        <path d="M3 12h1M12 3v1M20 12h1M5.6 5.6l.7 .7M18.4 5.6l-.7 .7"/>
        <rect x="6" y="16" width="12" height="4" rx="1"/>
      </svg>
    </span>`
  );
