﻿import {h} from "@stencil/core";

export const WriteHttpResponseIcon = props =>
  (
    <span class={`rounded-lg inline-flex p-3 bg-light-blue-50 text-light-blue-700 ring-4 ring-white`}>
      <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
      </svg>
    </span>
  );
