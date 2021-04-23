import {h} from "@stencil/core";

export const ReadLineIcon = props =>
  (
    `<span class="rounded-lg inline-flex p-3 bg-light-blue-50 text-light-blue-700 ring-4 ring-white">
      <svg class="h-6 w-6" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <polyline points="5 7 10 12 5 17"/>
        <line x1="13" y1="17" x2="19" y2="17"/>
      </svg>
    </span>`
  );
