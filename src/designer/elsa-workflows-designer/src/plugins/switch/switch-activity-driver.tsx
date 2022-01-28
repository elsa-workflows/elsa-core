import 'reflect-metadata';
import {h} from "@stencil/core";
import {Container, Service} from "typedi";
import {ActivityTraits} from '../../models';
import {ActivityDisplayContext, ActivityDriver, ActivityIcon, ActivityIconRegistry} from '../../services';

@Service()
export class SwitchActivityDriver implements ActivityDriver {

  display(context: ActivityDisplayContext): any {
    const activityDescriptor = context.activityDescriptor;
    const text = activityDescriptor?.displayName;

    return (`
          <div>
            <div class="activity-wrapper border border-sky-600 bg-sky-400 rounded text-white overflow-hidden">
              <div class="activity-content-wrapper flex flex-row">
                <div class="flex items-center">
                  <div class="px-4 py-4">
                    ${text}
                  </div>
                </div>
              </div>
            </div>
          </div>
        `);
  }
}
