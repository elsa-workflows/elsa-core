import { ActivityDescriptor, ActivityModel } from '../../../models';

export interface Port {
  name: string;
  displayName: string;
  mode: PortMode;
}

export interface PortProviderContext {
    activityDescriptor: ActivityDescriptor;
  activity: ActivityModel;
}

export enum PortMode {
  Embedded = 'Embedded',
  Port = 'Port'
}

export interface FlowchartNavigationItem {
  activityId: string;
  portName?: string;
  index: number;
}

export interface ActivityX6 {
  id: string;
  type: string;
  version: number;
  metadata: any;
  canStartWorkflow?: boolean;
  runAsynchronously?: boolean;
  applicationProperties: any;

  [name: string]: any;
}
