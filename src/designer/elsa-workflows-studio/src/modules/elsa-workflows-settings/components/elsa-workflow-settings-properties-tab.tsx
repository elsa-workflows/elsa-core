import { Component, h, Prop } from "@stencil/core";
import { propertyDisplayManager } from '../../../services';
import { ActivityModel, ActivityPropertyDescriptor } from "../../../models";

@Component({
    tag: 'elsa-workflow-settings-properties-tab',
    shadow: false,
})
export class ElsaWorkflowSettingsPropertiesTab {
    @Prop() activityModel: ActivityModel;
    @Prop() propertyDescriptor: ActivityPropertyDescriptor;

    render() {
        return(
                <div class="elsa-flex elsa-mt-1">
                <div class="elsa-space-y-8 elsa-w-full">
                    <div class={`elsa-grid elsa-grid-cols-1 elsa-gap-y-6 elsa-gap-x-4 sm:elsa-grid-cols-6`}>
                    {this.renderPropertyEditor(this.activityModel, this.propertyDescriptor)}
                    </div>
                </div>
                </div>
            )
    }

    renderPropertyEditor(activity: ActivityModel, property: ActivityPropertyDescriptor) {
        const key = `activity-property-input:${activity.activityId}:${property.name}`;
        const display = propertyDisplayManager.display(activity, property);
        const id = `${property.name}Control`;
        return <elsa-control key={key} id={id} class="sm:elsa-col-span-6" content={display}/>;
    }
}