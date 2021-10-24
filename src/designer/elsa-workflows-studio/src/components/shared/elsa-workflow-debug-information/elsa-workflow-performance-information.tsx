import { Component, h, Prop } from '@stencil/core';
import moment from 'moment';
import { durationToString } from '../../../utils/utils';
import { ActivityStats } from '../../..';

@Component({
  tag: 'elsa-workflow-performance-information',
  shadow: false,
})
export class ElsaWorkflowPerformanceInformation {
  @Prop() activityStats: ActivityStats

  render() {
      if (!this.activityStats) return undefined;

      return [
        <a href="#" class="-elsa-m-3 elsa-p-3 elsa-flex elsa-items-start elsa-rounded-lg hover:elsa-bg-gray-50 elsa-transition elsa-ease-in-out elsa-duration-150">
          <svg class="elsa-flex-shrink-0 elsa-h-6 elsa-w-6 elsa-text-blue-600" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <path stroke="none" d="M0 0h24v24H0z"/>
            <rect x="4" y="5" width="16" height="16" rx="2"/>
            <line x1="16" y1="3" x2="16" y2="7"/>
            <line x1="8" y1="3" x2="8" y2="7"/>
            <line x1="4" y1="11" x2="20" y2="11"/>
            <line x1="11" y1="15" x2="12" y2="15"/>
            <line x1="12" y1="15" x2="12" y2="18"/>
          </svg>
          <div class="elsa-ml-4">
            <p class="elsa-text-base elsa-font-medium elsa-text-gray-900">Average Execution Time</p>
            <p class="elsa-mt-1 elsa-text-sm elsa-text-gray-500">{durationToString(moment.duration(this.activityStats.averageExecutionTime))}</p>
          </div>
        </a>,

        <a href="#" class="-m-3 elsa-p-3 elsa-flex elsa-items-start elsa-rounded-lg hover:elsa-bg-gray-50 elsa-transition elsa-ease-in-out elsa-duration-150">
          <svg class="elsa-flex-shrink-0 elsa-h-6 elsa-w-6 elsa-text-green-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <path stroke="none" d="M0 0h24v24H0z"/>
            <circle cx="12" cy="12" r="9"/>
            <polyline points="12 7 12 12 15 15"/>
          </svg>
          <div class="elsa-ml-4">
            <p class="elsa-text-base elsa-font-medium elsa-text-gray-900">
              Fastest Execution Time
            </p>
            <p class="elsa-mt-1 elsa-text-sm elsa-text-gray-500">
              {durationToString(moment.duration(this.activityStats.fastestExecutionTime))}
            </p>
          </div>
        </a>,

        <a href="#" class="-elsa-m-3 elsa-p-3 elsa-flex elsa-items-start elsa-rounded-lg hover:elsa-bg-gray-50 elsa-transition elsa-ease-in-out elsa-duration-150">
          <svg class="elsa-flex-shrink-0 elsa-h-6 elsa-w-6 elsa-text-yellow-500" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <path stroke="none" d="M0 0h24v24H0z"/>
            <circle cx="12" cy="12" r="9"/>
            <polyline points="12 7 12 12 15 15"/>
          </svg>
          <div class="elsa-ml-4">
            <p class="elsa-text-base elsa-font-medium elsa-text-gray-900">
              Slowest Execution Time
            </p>
            <p class="elsa-mt-1 elsa-text-sm elsa-text-gray-500">
              {durationToString(moment.duration(this.activityStats.slowestExecutionTime))}
            </p>
          </div>
        </a>,

        <a href="#" class="-elsa-m-3 elsa-p-3 elsa-flex elsa-items-start elsa-rounded-lg hover:elsa-bg-gray-50 elsa-transition elsa-ease-in-out elsa-duration-150">
          <svg class="elsa-flex-shrink-0 elsa-h-6 elsa-w-6 elsa-text-blue-600" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
            <path stroke="none" d="M0 0h24v24H0z"/>
            <rect x="4" y="5" width="16" height="16" rx="2"/>
            <line x1="16" y1="3" x2="16" y2="7"/>
            <line x1="8" y1="3" x2="8" y2="7"/>
            <line x1="4" y1="11" x2="20" y2="11"/>
            <line x1="11" y1="15" x2="12" y2="15"/>
            <line x1="12" y1="15" x2="12" y2="18"/>
          </svg>
          <div class="elsa-ml-4">
            <p class="elsa-text-base elsa-font-medium elsa-text-gray-900">
              Last Executed At
            </p>
            <p class="elsa-mt-1 elsa-text-sm elsa-text-gray-500">
              {moment(this.activityStats.lastExecutedAt).format('DD-MM-YYYY HH:mm:ss')}
            </p>
          </div>
        </a>];
  }
}
