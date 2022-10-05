export interface Port {
  name: string;
  displayName: string;
  mode: PortMode;
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
