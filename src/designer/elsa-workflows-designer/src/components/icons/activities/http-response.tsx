import {FunctionalComponent, h} from '@stencil/core';
import {ActivityIconSettings, getActivityIconCssClass} from "./models";

export const HttpResponseIcon: FunctionalComponent<ActivityIconSettings> = (settings) => (
  <svg class={getActivityIconCssClass(settings)} width="24" height="24" viewBox="0 0 24 24"
       stroke-width="2"
       stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
  </svg>
);
