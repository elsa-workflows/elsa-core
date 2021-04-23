import {h} from "@stencil/core";

export const EraseIcon = props =>
  (
    `<span class="${`rounded-lg inline-flex p-3 bg-light-blue-50 text-light-blue-700 ring-4 ring-white`}">
      <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <path d="M19 19h-11l-4 -4a1 1 0 0 1 0 -1.41l10 -10a1 1 0 0 1 1.41 0l5 5a1 1 0 0 1 0 1.41l-9 9"/>
        <path d="M18 12.3l-6.3 -6.3"/>
      </svg>
    </span>`
  );
