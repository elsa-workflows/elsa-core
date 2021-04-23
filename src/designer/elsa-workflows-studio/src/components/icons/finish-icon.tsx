import {h} from "@stencil/core";

export const FinishIcon = props =>
  (
    `<span class="${`rounded-lg inline-flex p-3 bg-light-blue-50 text-light-blue-700 ring-4 ring-white`}">
      <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z"/>
        <line x1="4" y1="22" x2="4" y2="15"/>
      </svg>
    </span>`
  );
