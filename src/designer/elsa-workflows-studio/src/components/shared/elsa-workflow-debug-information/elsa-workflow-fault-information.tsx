import { Component, h, Prop } from '@stencil/core';
import moment from 'moment';
import { clip } from '../../../utils/utils';
import { SimpleException, WorkflowFault } from '../../../models';

@Component({
  tag: 'elsa-workflow-fault-information',
  shadow: false,
})
export class ElsaWorkflowFaultInformation {
  @Prop() workflowFault: WorkflowFault;
  @Prop() faultedAt: Date;

  render() {
    if (!this.workflowFault) return undefined;

    const renderExceptionMessage = (exception: SimpleException) => {
      return (
        <div>
          <div class="elsa-mb-4">
            <strong class="elsa-block elsa-font-bold">{exception.type}</strong>
            {exception.message}
          </div>
          {!!exception.innerException ? <div class="elsa-ml-4">{renderExceptionMessage(exception.innerException)}</div> : undefined}
        </div>
      );
    };

    return [
      <div class="-elsa-m-3 elsa-p-3 elsa-flex elsa-items-start elsa-rounded-lg hover:elsa-bg-gray-50 elsa-transition elsa-ease-in-out elsa-duration-150">
        <svg
          class="elsa-flex-shrink-0 elsa-h-6 elsa-w-6 elsa-text-red-600"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <circle cx="12" cy="12" r="10" />
          <line x1="12" y1="8" x2="12" y2="12" />
          <line x1="12" y1="16" x2="12.01" y2="16" />
        </svg>
        <div class="elsa-ml-4">
          <p class="elsa-text-base elsa-font-medium elsa-text-gray-900">Fault</p>
          <p class="elsa-mt-1 elsa-text-sm elsa-text-gray-500">
            {renderExceptionMessage(this.workflowFault.exception)}

            <pre class="elsa-overflow-x-scroll elsa-max-w-md" onClick={e => clip(e.currentTarget)}>
              {JSON.stringify(this.workflowFault, null, 1)}
            </pre>
          </p>
        </div>
      </div>,

      <a href="#" class="-elsa-m-3 elsa-p-3 elsa-flex elsa-items-start elsa-rounded-lg hover:elsa-bg-gray-50 elsa-transition elsa-ease-in-out elsa-duration-150">
        <svg
          class="elsa-flex-shrink-0 elsa-h-6 elsa-w-6 elsa-text-blue-600"
          width="24"
          height="24"
          viewBox="0 0 24 24"
          stroke-width="2"
          stroke="currentColor"
          fill="none"
          stroke-linecap="round"
          stroke-linejoin="round"
        >
          <path stroke="none" d="M0 0h24v24H0z" />
          <rect x="4" y="5" width="16" height="16" rx="2" />
          <line x1="16" y1="3" x2="16" y2="7" />
          <line x1="8" y1="3" x2="8" y2="7" />
          <line x1="4" y1="11" x2="20" y2="11" />
          <line x1="11" y1="15" x2="12" y2="15" />
          <line x1="12" y1="15" x2="12" y2="18" />
        </svg>
        <div class="elsa-ml-4">
          <p class="elsa-text-base elsa-font-medium elsa-text-gray-900">Faulted At</p>
          <p class="elsa-mt-1 elsa-text-sm elsa-text-gray-500">{moment(this.faultedAt).format('DD-MM-YYYY HH:mm:ss')}</p>
        </div>
      </a>,
    ];
  }
}
