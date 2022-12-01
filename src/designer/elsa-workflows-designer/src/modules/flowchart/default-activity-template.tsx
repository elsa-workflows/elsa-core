import {Component, h, Prop, State, Event, EventEmitter, Listen, Element} from "@stencil/core";
import {ActivityIconProducer, ActivityIconRegistry, PortProviderRegistry} from "../../services";
import {Container} from "typedi";
import {Activity, ActivityDescriptor, ActivityKind, ActivitySelectedArgs, ChildActivitySelectedArgs, EditChildActivityArgs, Port, PortMode} from "../../models";
import descriptorsStore from "../../data/descriptors-store";
import {isNullOrWhitespace} from "../../utils";
import FlowchartTunnel from "./state";

@Component({
  tag: 'elsa-default-activity-template',
  shadow: false
})
export class DefaultActivityTemplate {
  private readonly iconRegistry: ActivityIconRegistry;
  private readonly portProviderRegistry: PortProviderRegistry;
  private activityDescriptor: ActivityDescriptor;
  private icon: ActivityIconProducer;
  private portElements: Array<HTMLElement> = [];

  constructor() {
    this.iconRegistry = Container.get(ActivityIconRegistry);
    this.portProviderRegistry = Container.get(PortProviderRegistry);
  }

  @Prop({attribute: 'activity-type'}) activityType: string;
  @Prop({attribute: 'activity-type-version'}) activityTypeVersion: number = 1;
  @Prop({attribute: 'display-type'}) displayType: string;
  @Prop({attribute: 'activity-id'}) activityId: string;
  @Event() editChildActivity: EventEmitter<EditChildActivityArgs>;
  @Event() childActivitySelected: EventEmitter<ChildActivitySelectedArgs>;
  @State() private selectedPortName: string;

  componentWillLoad() {
    const iconRegistry = this.iconRegistry;
    const activityType = this.activityType;
    const activityTypeVersion = this.activityTypeVersion ?? 0;
    this.activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.typeName == activityType && x.version == activityTypeVersion);
    this.icon = iconRegistry.has(activityType) ? iconRegistry.get(activityType) : null;
  }

  componentWillRender() {
    this.portElements = [];
  }

  render() {
    const activityDescriptor = this.activityDescriptor;
    const activityId = this.activityId;
    const portProvider = this.portProviderRegistry.get(activityDescriptor.typeName);

    return (
      <FlowchartTunnel.Consumer>
        {({nodeMap}) => {
          const activity: Activity = nodeMap[activityId];
          const ports = portProvider.getOutboundPorts({activityDescriptor, activity});
          const embeddedPorts = ports.filter(x => x.mode == PortMode.Embedded);
          const hasEmbeddedPorts = embeddedPorts.length > 0;
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

          if (embeddedPorts.length == 0 || displayTypeIsPicker) {
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
                          {this.renderPorts(activity, embeddedPorts)}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            );
          } else {
            return (
              <div>
                <div class={`activity-wrapper border ${borderColor} ${backgroundColor} ${containerCssClass} rounded overflow-hidden`}>
                  <div class="text-white">
                    <div class={`flex flex-shrink items-center py-3 pr-3 ${iconBackgroundColor}`}>
                      {this.renderIcon(icon)}
                      <span>{displayText}</span>
                    </div>
                  </div>
                  <div class="activity-content-wrapper flex flex-col">
                    <div class="flex items-center">
                      <div class={contentCssClass}>
                        {this.renderPorts(activity, embeddedPorts)}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            );
          }
        }}
      </FlowchartTunnel.Consumer>
    )
  }

  private renderIcon = (icon?: ActivityIconProducer): string => {
    const iconCssClass = this.displayTypeIsPicker ? 'px-2' : 'px-4';

    if (!icon)
      return <div class={iconCssClass}></div>;

    return (
      <div class={`${iconCssClass} py-1`}>
        {icon()}
      </div>
    );
  }

  private renderPorts = (activity: Activity, embeddedPorts: Port[]) => {

    if (this.displayTypeIsPicker || !this.activityDescriptor)
      return;

    if (embeddedPorts.length == 0)
      return;

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
    const activityDescriptor = this.activityDescriptor;
    const portProvider = this.portProviderRegistry.get(activityDescriptor.typeName);
    const activityProperty = portProvider.resolvePort(port.name, {activity, activityDescriptor}) as Activity;
    const childActivityDescriptor: ActivityDescriptor = activityProperty != null ? descriptorsStore.activityDescriptors.find(x => x.typeName == activityProperty.type) : null;
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
