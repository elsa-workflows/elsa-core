import {Component, h, Prop, State, Event, EventEmitter, Listen, Element} from "@stencil/core";
import {camelCase} from 'lodash';
import {ActivityIcon, ActivityIconRegistry} from "../../../services";
import {Container} from "typedi";
import {Activity, ActivityDescriptor, ActivityKind, ActivitySelectedArgs, ChildActivitySelectedArgs, EditChildActivityArgs, Port, PortMode} from "../../../models";
import descriptorsStore from "../../../data/descriptors-store";
import {isNullOrWhitespace} from "../../../utils";
import WorkflowEditorTunnel from "../state";

@Component({
  tag: 'elsa-default-activity-template',
  shadow: false
})
export class DefaultActivityTemplate {
  private readonly iconRegistry: ActivityIconRegistry;
  private activityDescriptor: ActivityDescriptor;
  private icon: ActivityIcon;
  private portElements: Array<HTMLElement> = [];

  constructor() {
    this.iconRegistry = Container.get(ActivityIconRegistry);
  }

  @Prop({attribute: 'activity-type'}) activityType: string;
  @Prop({attribute: 'display-type'}) displayType: string;
  @Prop({attribute: 'activity-id'}) activityId: string;
  @Event() editChildActivity: EventEmitter<EditChildActivityArgs>;
  @Event() childActivitySelected: EventEmitter<ChildActivitySelectedArgs>;
  @State() private selectedPortName: string;

  componentWillLoad() {
    const iconRegistry = this.iconRegistry;
    this.activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == this.activityType);
    const activityType = this.activityType;
    this.icon = iconRegistry.has(activityType) ? iconRegistry.get(activityType) : null;
  }

  componentWillRender() {
    this.portElements = [];
  }

  render() {
    const activityDescriptor = this.activityDescriptor;
    const activityId = this.activityId;

    return (
      <WorkflowEditorTunnel.Consumer>
        {({nodeMap}) => {
          const activity: Activity = nodeMap[activityId];
          const canStartWorkflow = activity?.canStartWorkflow;
          const icon = this.icon;
          const textColor = canStartWorkflow ? 'text-white' : 'text-gray-700';
          const isTrigger = activityDescriptor?.kind == ActivityKind.Trigger;
          const backgroundColor = canStartWorkflow ? isTrigger ? 'bg-green-400' : 'bg-blue-400' : 'bg-white';
          const iconBackgroundColor = isTrigger ? 'bg-green-500' : 'bg-blue-500';
          const borderColor = canStartWorkflow ? isTrigger ? 'border-green-600' : 'border-blue-600' : 'border-gray-300';
          const displayTypeIsPicker = this.displayTypeIsPicker;
          const displayTypeIsEmbedded = this.displayTypeIsEmbedded;
          const containerCssClass = displayTypeIsEmbedded ? '' : 'drop-shadow-md';
          const contentCssClass = displayTypeIsPicker ? 'px-2 py-2' : 'px-4 py-4';
          let displayText = activity?.metadata?.displayText;

          if (isNullOrWhitespace(displayText))
            displayText = activityDescriptor?.displayName;

          return (
            <div>
              <div class={`activity-wrapper border ${borderColor} ${backgroundColor} ${containerCssClass} rounded text-white overflow-hidden`}>
                <div class="activity-content-wrapper flex flex-row">
                  <div class={`flex flex-shrink items-center ${iconBackgroundColor}`}>
                    {this.renderIcon(icon)}
                  </div>
                  <div class="flex items-center">
                    <div class={contentCssClass}>
                      <span class={textColor}>{displayText}</span>
                      <div>
                        {this.renderPorts(activity)}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          );
        }}
      </WorkflowEditorTunnel.Consumer>
    )
  }

  private renderIcon = (icon?: ActivityIcon): string => {
    const iconCssClass = this.displayTypeIsPicker ? 'px-2' : 'px-4';

    if (!icon)
      return undefined;

    return (
      <div class={`${iconCssClass} py-1`}>
        {icon()}
      </div>
    );
  }

  private renderPorts = (activity: Activity) => {

    if (this.displayTypeIsPicker)
      return undefined;

    const activityDescriptor = this.activityDescriptor;
    const ports = activityDescriptor?.ports ?? [];
    const embeddedPorts = ports.filter(x => x.mode == PortMode.Embedded);

    if (embeddedPorts.length == 0)
      return undefined;

    return (
      <div class="activity-ports mt-2 flex space-x-2">
        {embeddedPorts.map(port => this.renderPort(activity, port))}
      </div>
    );
  };

  private renderPort = (activity: Activity, port: Port) => {
    const canStartWorkflow = activity?.canStartWorkflow == true;
    const textColor = canStartWorkflow ? 'text-white' : 'text-gray-700';
    const borderColor = port.name == this.selectedPortName ? 'border-blue-600' : 'border-gray-300';
    const portName = camelCase(port.name);
    const activityProperty: Activity = activity ? activity[portName] : null;
    const childActivityDescriptor: ActivityDescriptor = activityProperty != null ? descriptorsStore.activityDescriptors.find(x => x.activityType == activityProperty.typeName) : null;
    let childActivityDisplayText = activityProperty?.metadata?.displayText;

    if (isNullOrWhitespace(childActivityDisplayText))
      childActivityDisplayText = childActivityDescriptor?.displayName;

    const renderActivityProperty = () => {

      if (!activityProperty) {
        return (
          <div class="relative block w-full border-2 border-gray-300 border-dashed rounded-lg p-5 text-center focus:outline-none">
            <a href="#"
               onClick={e => this.onEditChildActivityClick(e, activity, port)}
               onMouseDown={e => e.stopPropagation()}
               class="text-gray-400 hover:text-gray-600">
              <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
              </svg>
            </a>
          </div>
        );
      }

      const propertyIsArray = Array.isArray(activityProperty);

      if (!propertyIsArray) {
        return (
          <div class={`relative block w-full border-2 ${borderColor} border-solid rounded-lg p-5 text-center focus:outline-none`}
               onMouseDown={this.onChildActivityMouseDown}
               onClick={e => this.onChildActivityClick(e, activity, activityProperty, port)}>
            <div class="flex space-x-2">
              <div class="flex-grow">
                <span class={textColor}>{childActivityDisplayText}</span>
              </div>
              <div class="flex-shrink">
                <a
                  onClick={e => this.onEditChildActivityClick(e, activity, port)}
                  onMouseDown={e => e.stopPropagation()}
                  href="#"
                  class="text-gray-500 hover:text-yellow-700">
                  <svg class="h-6 w-6" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                    <path stroke="none" d="M0 0h24v24H0z"/>
                    <path d="M4 20h4l10.5 -10.5a1.5 1.5 0 0 0 -4 -4l-10.5 10.5v4"/>
                    <line x1="13.5" y1="6.5" x2="17.5" y2="10.5"/>
                  </svg>
                </a>
              </div>
            </div>
          </div>
        );
      }

      return (
        <div class={`relative block w-full border-2 ${borderColor} border-solid rounded-lg p-5 text-center focus:outline-none`}>
          <div class="flex space-x-2">
            <div class="flex-grow">
              <span class={textColor}>{childActivityDisplayText}</span>
            </div>
            <div class="flex-shrink">
              <a
                onClick={e => this.onEditChildActivityClick(e, activity, port)}
                onMouseDown={e => e.stopPropagation()}
                href="#"
                class="text-gray-500 hover:text-yellow-700">
                <svg class="h-6 w-6" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                  <path stroke="none" d="M0 0h24v24H0z"/>
                  <path d="M4 20h4l10.5 -10.5a1.5 1.5 0 0 0 -4 -4l-10.5 10.5v4"/>
                  <line x1="13.5" y1="6.5" x2="17.5" y2="10.5"/>
                </svg>
              </a>
            </div>
          </div>
        </div>
      );
    }

    return (
      <div class="activity-port" data-port-name={port.name} ref={el => this.portElements.push(el)}>
        <div>
          <span class={`${textColor} text-xs`}>{port.displayName}</span>
        </div>
        <div>
          {renderActivityProperty()}
        </div>
      </div>
    );
  };

  private get displayTypeIsPicker(): boolean {
    return this.displayType == "picker";
  }

  private get displayTypeIsEmbedded(): boolean {
    return this.displayType == "embedded";
  }

  @Listen('click', {target: 'window'})
  private onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    for (const portElement of this.portElements)
      if (portElement.contains(target))
        return;

    this.selectedPortName = null;
  }

  private onEditChildActivityClick = (e: MouseEvent, parentActivity: Activity, port: Port) => {
    e.preventDefault();
    this.editChildActivity.emit({parentActivityId: parentActivity.id, port: port});
  };

  private onChildActivityMouseDown = (e: MouseEvent) => {
    // Prevent X6 from capturing the click event.
    e.stopPropagation();
  };

  private onChildActivityClick = (e: MouseEvent, parentActivity: Activity, childActivity: Activity, port: Port) => {
    const args: ChildActivitySelectedArgs = {
      parentActivity: parentActivity,
      childActivity: childActivity,
      port: port
    };

    this.selectedPortName = port.name;
    this.childActivitySelected.emit(args);
  };
}
