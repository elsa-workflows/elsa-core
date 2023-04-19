export interface TabDefinition {
  displayText: string;
  content: () => any;
  order?: number;
}

export interface TabChangedArgs {
  selectedTabIndex: number;
}

export interface SelectList {
  items: Array<SelectListItem> | Array<string>;
  isFlagsEnum?: boolean;
}

export interface SelectListItem {
  text: string;
  value: string;
}

export interface RuntimeSelectListProviderSettings {
  runtimeSelectListProviderType: string;
  context?: any;
}

export enum EditorHeight {
  Default = 'Default',
  Large = 'Large'
}
