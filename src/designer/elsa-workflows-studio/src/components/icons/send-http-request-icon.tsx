import {h} from "@stencil/core";

export const SendHttpRequestIcon = props =>
  (
    `<span class="${`elsa-rounded-lg elsa-inline-flex elsa-p-3 elsa-bg-sky-50 elsa-text-sky-700 elsa-ring-4 elsa-ring-white`}">
      <svg class="elsa-h-6 elsa-w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width={2} d="M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z"/>
      </svg>
    </span>`
  );
