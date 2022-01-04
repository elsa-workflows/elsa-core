import {h} from "@stencil/core";
import 'reflect-metadata';
import {ActivityTraits} from '../../models';
import {ActivityDisplayContext, ActivityDriver} from '../../services';
import {Service} from "typedi";
import {ActivityIcon, ActivityIconRegistry} from "../../services/activity-icon-registry";

@Service()
export class DefaultActivityDriver implements ActivityDriver {
  constructor(private iconRegistry: ActivityIconRegistry) {
  }

  display(context: ActivityDisplayContext): any {
    const activityDescriptor = context.activityDescriptor;
    const nodeType = activityDescriptor.nodeType;
    const text = activityDescriptor?.displayName;
    const isTrigger = (activityDescriptor?.traits & ActivityTraits.Trigger) == ActivityTraits.Trigger;
    const borderColor = isTrigger ? 'border-green-600' : 'border-blue-600';
    const backgroundColor = isTrigger ? 'bg-green-400' : 'bg-blue-400';
    const iconBackgroundColor = isTrigger ? 'bg-green-500' : 'bg-blue-500';
    const icon = this.iconRegistry.has(nodeType) ? this.iconRegistry.get(nodeType) : null;

    return (`
          <div>
            <div class="activity-wrapper border ${borderColor} ${backgroundColor} rounded text-white overflow-hidden">
              <div class="activity-content-wrapper flex flex-row">
                <div class="flex flex-shrink items-center ${iconBackgroundColor}">
                ${this.renderIcon(icon)}
                </div>
                <div class="flex items-center">
                  <div class="px-4 py-1">
                    ${text}
                  </div>
                </div>
              </div>
            </div>
          </div>
        `);
  }

  private renderIcon = (icon?: ActivityIcon): string => {
    if (!icon)
      return '';

    return (
      `<div class="px-2 py-1">
        ${icon()}
      </div>`
    );
  }

}
