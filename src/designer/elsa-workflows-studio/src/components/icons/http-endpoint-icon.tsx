import {h} from "@stencil/core";

export const HttpEndpointIcon = props =>
  (
    `<span class="${`rounded-lg inline-flex p-3 bg-rose-50 text-rose-700 ring-4 ring-white`}">
      <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width={2} d="M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z"/>
      </svg>
    </span>`
  );
